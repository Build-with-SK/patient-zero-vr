using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace PatientZero.Core.Tests
{
    public class EventChannelTests
    {
        private struct Ping
        {
            public int Value;
        }

        [Test]
        public void Raise_DeliversPayloadToEverySubscriber()
        {
            var channel = new EventChannel<Ping>("Test");
            int a = 0, b = 0;
            channel.Subscribe(p => a += p.Value);
            channel.Subscribe(p => b += p.Value * 10);

            channel.Raise(new Ping { Value = 2 });

            Assert.AreEqual(2, a);
            Assert.AreEqual(20, b);
        }

        [Test]
        public void Subscribe_SameHandlerTwice_RunsOnce()
        {
            var channel = new EventChannel<Ping>("Test");
            int calls = 0;
            Action<Ping> handler = p => calls++;
            channel.Subscribe(handler);
            channel.Subscribe(handler);

            channel.Raise(default(Ping));

            Assert.AreEqual(1, calls);
            Assert.AreEqual(1, channel.SubscriberCount);
        }

        [Test]
        public void Unsubscribe_MethodGroup_RemovesEqualDelegate()
        {
            var channel = new EventChannel<Ping>("Test");
            var listener = new Listener();
            channel.Subscribe(listener.OnPing);
            channel.Unsubscribe(listener.OnPing); // a different delegate instance, same target and method

            channel.Raise(default(Ping));

            Assert.AreEqual(0, listener.Calls);
            Assert.AreEqual(0, channel.SubscriberCount);
        }

        [Test]
        public void Unsubscribe_DuringRaise_SkipsRemovedHandler()
        {
            var channel = new EventChannel<Ping>("Test");
            var order = new List<string>();
            Action<Ping> second = p => order.Add("second");
            channel.Subscribe(p =>
            {
                order.Add("first");
                channel.Unsubscribe(second);
            });
            channel.Subscribe(second);

            channel.Raise(default(Ping));

            Assert.AreEqual(1, order.Count);
            Assert.AreEqual("first", order[0]);
            Assert.AreEqual(1, channel.SubscriberCount);
        }

        [Test]
        public void Unsubscribe_EarlierHandlerDuringRaise_LaterHandlersStillRun()
        {
            // Removing an earlier entry must not shift a later handler past the loop index.
            var channel = new EventChannel<Ping>("Test");
            var order = new List<string>();
            Action<Ping> first = p => order.Add("first");
            channel.Subscribe(first);
            channel.Subscribe(p =>
            {
                order.Add("second");
                channel.Unsubscribe(first);
            });
            channel.Subscribe(p => order.Add("third"));

            channel.Raise(default(Ping));

            Assert.AreEqual(3, order.Count);
            Assert.AreEqual("third", order[2]);
            Assert.AreEqual(2, channel.SubscriberCount);
        }

        [Test]
        public void Unsubscribe_SelfDuringRaise_CompactsAfterwards()
        {
            var channel = new EventChannel<Ping>("Test");
            int calls = 0;
            Action<Ping> once = null;
            once = p =>
            {
                calls++;
                channel.Unsubscribe(once);
            };
            channel.Subscribe(once);

            channel.Raise(default(Ping));
            channel.Raise(default(Ping));

            Assert.AreEqual(1, calls);
            Assert.AreEqual(0, channel.SubscriberCount);
        }

        [Test]
        public void Subscribe_DuringRaise_RunsFromNextRaise()
        {
            var channel = new EventChannel<Ping>("Test");
            int late = 0;
            bool added = false;
            channel.Subscribe(p =>
            {
                if (added) return;
                added = true;
                channel.Subscribe(q => late++);
            });

            channel.Raise(default(Ping));
            Assert.AreEqual(0, late);

            channel.Raise(default(Ping));
            Assert.AreEqual(1, late);
        }

        [Test]
        public void HandlerThrows_RemainingHandlersStillRun()
        {
            var channel = new EventChannel<Ping>("Test");
            int after = 0;
            channel.Subscribe(p => { throw new InvalidOperationException("boom"); });
            channel.Subscribe(p => after++);

            LogAssert.Expect(LogType.Exception, new Regex("boom"));
            channel.Raise(default(Ping));

            Assert.AreEqual(1, after);
        }

        [Test]
        public void NestedRaise_DeliversInnerAndOuter()
        {
            var channel = new EventChannel<Ping>("Test");
            var seen = new List<int>();
            channel.Subscribe(p =>
            {
                seen.Add(p.Value);
                if (p.Value == 1) channel.Raise(new Ping { Value = 2 });
            });

            channel.Raise(new Ping { Value = 1 });

            Assert.AreEqual(2, seen.Count);
            Assert.AreEqual(1, seen[0]);
            Assert.AreEqual(2, seen[1]);
        }

        [Test]
        public void Clear_DuringRaise_DoesNotThrowAndStopsLaterHandlers()
        {
            var channel = new EventChannel<Ping>("Test");
            int later = 0;
            channel.Subscribe(p => channel.Clear());
            channel.Subscribe(p => later++);

            Assert.DoesNotThrow(() => channel.Raise(default(Ping)));
            Assert.AreEqual(0, later);
            Assert.AreEqual(0, channel.SubscriberCount);
        }

        [Test]
        public void Subscribe_Null_Throws()
        {
            var channel = new EventChannel<Ping>("Test");
            Assert.Throws<ArgumentNullException>(() => channel.Subscribe(null));
        }

        private sealed class Listener
        {
            public int Calls;

            public void OnPing(Ping ping)
            {
                Calls++;
            }
        }
    }
}
