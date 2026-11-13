# Synthesises the revolver sounds for PATIENT ZERO (E-25). Own work: no samples, no third-party material.
import numpy as np, wave, sys
SR = 44100

def band(x, lo, hi):
    X = np.fft.rfft(x); f = np.fft.rfftfreq(len(x), 1 / SR)
    m = np.ones_like(f)
    if lo: m *= 1 / (1 + (lo / np.maximum(f, 1)) ** 4)
    if hi: m *= 1 / (1 + (f / hi) ** 4)
    return np.fft.irfft(X * m, len(x))

def env(n, tau, start=0):
    t = np.arange(n) / SR - start
    return np.where(t >= 0, np.exp(-np.maximum(t, 0) / tau), 0)

def norm(x, peak=.89):
    return x / np.max(np.abs(x)) * peak

def save(name, x):
    x = np.clip(x, -1, 1)
    with wave.open(name, "wb") as w:
        w.setnchannels(1); w.setsampwidth(2); w.setframerate(SR)
        w.writeframes((x * 32767).astype("<i2").tobytes())

def shot(seed):
    r = np.random.default_rng(seed); n = int(SR * 1.7); t = np.arange(n) / SR
    pitch = 1 + r.uniform(-.06, .06)
    crack = band(r.standard_normal(n), 1500, 9000) * env(n, .004) * 1.4
    snap = band(r.standard_normal(n), 600, 4000) * env(n, .022) * 1.0
    f = (55 + 95 * np.exp(-t / .035)) * pitch
    boom = np.sin(2 * np.pi * np.cumsum(f) / SR) * env(n, .09) * 1.1
    body = band(r.standard_normal(n), 40, 700) * env(n, .07) * 1.3
    dry = crack + snap + boom + body
    wet = np.zeros(n)
    for d, g in ((.007, .55), (.013, .42), (.021, .33), (.029, .26), (.041, .2), (.057, .15)):
        k = int(d * SR); wet[k:] += dry[:n - k] * g * r.uniform(.8, 1.1)
    tail = band(r.standard_normal(n), 120, 2200) * env(n, .38, .012) * .22
    x = np.tanh(1.6 * (dry + .6 * wet + tail))
    fade = np.ones(n); fade[-int(.15 * SR):] = np.linspace(1, 0, int(.15 * SR))
    return norm(x * fade, .95)

def click(r, n, at, freqs, taus, gain):
    t = np.arange(n) / SR
    x = band(r.standard_normal(n), 2500, 9000) * env(n, .0012, at) * .8
    for f, tau in zip(freqs, taus):
        x += np.sin(2 * np.pi * f * (t - at)) * env(n, tau, at) * .5
    return x * gain

def dry_fire():
    r = np.random.default_rng(7); n = int(SR * .35)
    x = click(r, n, .0, (3100, 4700), (.004, .003), .35)           # the cylinder turns a notch
    x += click(r, n, .085, (2650, 3900, 6100), (.012, .008, .005), 1)  # the hammer falls on a spent chamber
    return norm(x, .8)

def handle():
    r = np.random.default_rng(11); n = int(SR * .45); t = np.arange(n) / SR
    x = click(r, n, .0, (900, 1750), (.025, .015), .7)              # the grip knocks the floor tiles as it is lifted
    x += band(r.standard_normal(n), 300, 3000) * env(n, .06, .02) * .25  # cloth and leather
    x += click(r, n, .19, (2200, 3300), (.01, .006), .45)           # the cylinder settles
    return norm(x, .7)

for i, seed in enumerate((101, 202, 303), 1): save("Revolver_Shot_%d.wav" % i, shot(seed))
save("Revolver_DryFire.wav", dry_fire())
save("Revolver_Handle.wav", handle())
print("ok")
