// Claude-owned integration (owner's step 1 feedback, 2026-11-11; E-23).
using NUnit.Framework;
using PatientZero.Integration.Logic;

namespace PatientZero.Integration.Tests
{
    public class LookAwayRuleTests
    {
        private const float Frame = .1f;

        private static LookAwayEvent Run(LookAwayRule rule, float seconds, float degrees, float meters = 3, bool clear = true)
        {
            LookAwayEvent last = LookAwayEvent.None;
            for (float t = 0; t < seconds - 1e-4f; t += Frame)
            {
                LookAwayEvent e = rule.Tick(Frame, degrees, meters, clear);
                if (e != LookAwayEvent.None) last = e;
            }

            return last;
        }

        [Test]
        public void TheBodyAppearsOnlyAfterTheSpotWasSeenEmptyAndThenLeftOutOfSight()
        {
            var rule = new LookAwayRule();
            Assert.AreEqual(LookAwayEvent.None, Run(rule, 5, 90), "never seen: looking away alone does nothing");
            Assert.AreEqual(LookAwayRule.Phase.Empty, rule.State);
            Run(rule, .5f, 10);
            Assert.AreEqual(LookAwayRule.Phase.SeenEmpty, rule.State);
            Assert.AreEqual(LookAwayEvent.None, Run(rule, 1.2f, 90), "not away long enough");
            Assert.AreEqual(LookAwayEvent.Revealed, Run(rule, .5f, 90));
            Assert.IsTrue(rule.BodyShown);
            Assert.AreEqual(LookAwayEvent.Spotted, Run(rule, .1f, 20));
            Assert.AreEqual(LookAwayRule.Phase.Spotted, rule.State);
            Assert.AreEqual(LookAwayEvent.None, Run(rule, 3, 90), "spotted once only");
        }

        [Test]
        public void ItNeverAppearsWhileTheSpotIsInView()
        {
            var rule = new LookAwayRule();
            Run(rule, 1, 0);
            Assert.AreEqual(LookAwayEvent.None, Run(rule, 10, LookAwayRule.OutOfViewDegrees - 1), "at the edge of the view it is still visible");
            Assert.AreEqual(LookAwayRule.Phase.SeenEmpty, rule.State);
        }

        [Test]
        public void GlancingBackRestartsTheWait()
        {
            var rule = new LookAwayRule();
            Run(rule, 1, 0);
            Run(rule, 1.2f, 90);
            Run(rule, .1f, 50); // back into the edge of the view
            Assert.AreEqual(LookAwayEvent.None, Run(rule, 1.2f, 90));
            Assert.AreEqual(LookAwayEvent.Revealed, Run(rule, .4f, 90));
        }

        [Test]
        public void AWallBetweenCountsAsOutOfSightButFarAwayDoesNot()
        {
            var rule = new LookAwayRule();
            Run(rule, 1, 0);
            Assert.AreEqual(LookAwayEvent.None, Run(rule, 5, 90, LookAwayRule.MaxMeters + 1), "a player who has left the area");
            Assert.AreEqual(LookAwayEvent.Revealed, Run(rule, 2, 0, 3, clear: false));
            Assert.AreEqual(LookAwayEvent.None, Run(rule, 1, 0, 3, clear: false), "behind a wall it cannot be spotted");
            Assert.AreEqual(LookAwayEvent.Spotted, Run(rule, .1f, 0));
        }

        [Test]
        public void ANewLifeEmptiesTheSpotAgain()
        {
            var rule = new LookAwayRule();
            Run(rule, 1, 0);
            Run(rule, 2, 90);
            Assert.IsTrue(rule.BodyShown);
            rule.Reset();
            Assert.AreEqual(LookAwayRule.Phase.Empty, rule.State);
            Assert.IsFalse(rule.BodyShown);
        }

        [Test]
        public void WithoutSeenFirstLookingAwayIsEnough()
        {
            var rule = new LookAwayRule(seenFirst: false);
            Assert.AreEqual(LookAwayEvent.Revealed, Run(rule, 2, 120));
        }

        [Test]
        public void BadInputChangesNothing()
        {
            var rule = new LookAwayRule();
            Assert.AreEqual(LookAwayEvent.None, rule.Tick(float.NaN, 0, 1, true));
            Assert.AreEqual(LookAwayEvent.None, rule.Tick(-1, 0, 1, true));
            Assert.AreEqual(LookAwayEvent.None, rule.Tick(float.PositiveInfinity, 0, 1, true));
            Assert.AreEqual(LookAwayEvent.None, rule.Tick(1, float.NaN, 1, true));
            Assert.AreEqual(LookAwayRule.Phase.Empty, rule.State);
        }
    }
}
