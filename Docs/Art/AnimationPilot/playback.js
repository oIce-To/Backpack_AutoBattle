/* Standalone review prototype. All time values are simulation seconds. */
(function (root) {
  class Playback {
    constructor() { this.time = 0; this.attackAt = -Infinity; this.hitAt = -Infinity; this.label = ''; }
    attack(label) { this.attackAt = this.time; this.label = label; }
    hit(outcome) { if (outcome === 'health' || outcome === 'armor') this.hitAt = this.time; }
    advance(delta, speed = 1, paused = false) { if (!paused) this.time += Math.max(0, delta) * speed; }
    get attacking() { return this.time - this.attackAt < .3 - 1e-9; }
    get frame() {
      if (!this.attacking) return Math.floor(this.time / .4) % 4;
      const t = this.time - this.attackAt;
      return 4 + (t < .04 ? 0 : t < .12 ? 1 : t < .21 ? 2 : 3);
    }
    get shake() {
      const t = this.time - this.hitAt;
      return t < .15 ? Math.round(Math.sin(t / .15 * Math.PI * 6) * 6 * (1 - t / .15)) : 0;
    }
  }
  if (typeof module !== 'undefined') module.exports = Playback;
  else root.AnimationPlayback = Playback;
})(typeof window !== 'undefined' ? window : globalThis);
