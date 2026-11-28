using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Wolfsun
{
    /// <summary>
    /// Procedural card effects. Every Order maps to one of fourteen spell effects (lightning, fire, frost, shadow, rune, heal,
    /// ward, summon, slash, quake, wind, curse, draw, arrow) tinted by its faction; every Unit gets a faction-coloured entrance
    /// that is varied per card and extended for Guard, Rush and Mythic cards; each Prophecy has its own full-screen sequence.
    /// All positions are anchored coordinates on the effects layer (x right, y negative downward).
    /// </summary>
    public sealed partial class Game
    {
        private enum FxKind { Bolt, Fire, Frost, Shadow, Rune, Heal, Ward, Summon, Slash, Quake, Wind, Curse, Draw, Arrow }

        private static readonly Dictionary<string, FxKind> SpellFxById = new Dictionary<string, FxKind>
        {
            { "A03", FxKind.Ward }, { "A04", FxKind.Rune }, { "A07", FxKind.Ward }, { "A11", FxKind.Ward }, { "A12", FxKind.Bolt },
            { "A14", FxKind.Summon }, { "A16", FxKind.Slash }, { "A18", FxKind.Ward }, { "A20", FxKind.Draw },
            { "M03", FxKind.Wind }, { "M04", FxKind.Summon }, { "M07", FxKind.Wind }, { "M11", FxKind.Wind }, { "M12", FxKind.Rune },
            { "M13", FxKind.Summon }, { "M16", FxKind.Wind }, { "M18", FxKind.Curse }, { "M20", FxKind.Summon }, { "M22", FxKind.Ward },
            { "J03", FxKind.Slash }, { "J04", FxKind.Rune }, { "J07", FxKind.Draw }, { "J11", FxKind.Quake }, { "J14", FxKind.Frost },
            { "J17", FxKind.Quake }, { "J20", FxKind.Frost }, { "J22", FxKind.Fire },
            { "H02", FxKind.Shadow }, { "H04", FxKind.Heal }, { "H07", FxKind.Shadow }, { "H11", FxKind.Shadow }, { "H12", FxKind.Shadow },
            { "H14", FxKind.Summon }, { "H18", FxKind.Shadow }, { "H20", FxKind.Shadow }, { "H22", FxKind.Shadow },
            { "L02", FxKind.Curse }, { "L03", FxKind.Curse }, { "L07", FxKind.Wind }, { "L09", FxKind.Wind }, { "L11", FxKind.Rune },
            { "L14", FxKind.Curse }, { "L16", FxKind.Curse }, { "L17", FxKind.Fire }, { "L19", FxKind.Draw }, { "L20", FxKind.Slash },
            { "L21", FxKind.Curse },
            { "N03", FxKind.Heal }, { "N07", FxKind.Wind }, { "N08", FxKind.Arrow }, { "N12", FxKind.Summon }, { "N17", FxKind.Quake },
            { "N18", FxKind.Slash }, { "N20", FxKind.Heal }, { "N23", FxKind.Frost },
            { "X08", FxKind.Curse }, { "X09", FxKind.Fire }, { "X10", FxKind.Wind },
        };

        private static FxKind PickFx(Card card)
        {
            if (SpellFxById.TryGetValue(card.Id, out var kind)) return kind;
            string r = card.Rules ?? "";
            if (r.Contains("Summon")) return FxKind.Summon;
            if (r.Contains("Heal")) return FxKind.Heal;
            if (r.Contains("Shield")) return FxKind.Ward;
            if (r.Contains("Destroy")) return FxKind.Slash;
            if (r.Contains("damage"))
                return card.Faction == 'J' ? FxKind.Frost : card.Faction == 'H' ? FxKind.Shadow : card.Faction == 'A' ? FxKind.Bolt : FxKind.Fire;
            if (r.Contains("Draw")) return FxKind.Draw;
            if (r.Contains("Move") || r.Contains("Recall")) return FxKind.Wind;
            if (r.Contains("Attack") && r.Contains("-")) return FxKind.Curse;
            return FxKind.Rune;
        }

        /// <summary>Seconds from the start of the effect until the hit lands, so damage numbers can wait for it.</summary>
        private static float ImpactDelay(FxKind kind)
        {
            switch (kind)
            {
                case FxKind.Bolt: return .16f;
                case FxKind.Fire: return .42f;
                case FxKind.Frost: return .18f;
                case FxKind.Shadow: return .55f;
                case FxKind.Rune: return .3f;
                case FxKind.Heal: return .25f;
                case FxKind.Ward: return .2f;
                case FxKind.Summon: return .3f;
                case FxKind.Slash: return .2f;
                case FxKind.Quake: return .25f;
                case FxKind.Wind: return .35f;
                case FxKind.Curse: return .3f;
                case FxKind.Draw: return .3f;
                default: return .28f;
            }
        }

        private static bool WantsOwnHero(FxKind kind) => kind == FxKind.Heal || kind == FxKind.Draw || kind == FxKind.Ward || kind == FxKind.Rune;

        private static Color[] Pal(char faction)
        {
            switch (faction)
            {
                case 'A': return new[] { new Color(1f, .82f, .35f), new Color(.75f, .88f, 1f) };
                case 'M': return new[] { new Color(1f, .62f, .28f), new Color(1f, .9f, .6f) };
                case 'J': return new[] { new Color(.55f, .82f, 1f), new Color(.9f, .97f, 1f) };
                case 'H': return new[] { new Color(.45f, .9f, .65f), new Color(.62f, .45f, .92f) };
                case 'L': return new[] { new Color(.82f, .4f, 1f), new Color(1f, .5f, .8f) };
                default: return new[] { new Color(1f, .9f, .65f), new Color(1f, 1f, .9f) };
            }
        }

        // ---------------------------------------------------------------- primitives

        private sealed class Spark
        {
            public RectTransform Rt;
            public RawImage Img;
            public Vector2 Pos, Vel;
            public float Gravity, Life, Age, Size0, Size1, Spin, Rot, Delay;
            public Color C0, C1;
            public bool Streak, Dead;
        }

        private Spark Emit(List<Spark> list, RectTransform layer, Vector2 at, Vector2 vel, float life, float s0, float s1,
            Color c0, Color c1, float gravity = 0f, float delay = 0f, bool streak = false)
        {
            var img = Picture(layer, "Gen/Shadow", 0, 0, 10, 10, c0);
            img.raycastTarget = false;
            img.rectTransform.pivot = new Vector2(.5f, .5f);
            img.rectTransform.anchoredPosition = at;
            var p = new Spark
            {
                Rt = img.rectTransform, Img = img, Pos = at, Vel = vel, Life = Mathf.Max(.05f, life), Size0 = s0, Size1 = s1,
                C0 = c0, C1 = c1, Gravity = gravity, Delay = delay, Streak = streak, Rot = Random.value * 360f, Spin = Random.Range(-240f, 240f)
            };
            img.enabled = delay <= 0f;
            list.Add(p);
            return p;
        }

        /// <summary>Drives a particle list (which may keep growing) until it is empty and `hold` seconds have passed.</summary>
        private IEnumerator RunSparks(List<Spark> list, float hold)
        {
            float clock = 0f;
            while (true)
            {
                float dt = Time.unscaledDeltaTime;
                clock += dt;
                bool any = false;
                for (int i = 0; i < list.Count; i++)
                {
                    var p = list[i];
                    if (p.Dead) continue;
                    if (!p.Rt) { p.Dead = true; continue; }
                    any = true;
                    if (p.Delay > 0f) { p.Delay -= dt; continue; }
                    if (!p.Img.enabled) p.Img.enabled = true;
                    p.Age += dt;
                    if (p.Age >= p.Life) { Destroy(p.Rt.gameObject); p.Dead = true; continue; }
                    float k = p.Age / p.Life;
                    p.Vel += new Vector2(0f, p.Gravity) * dt;
                    p.Pos += p.Vel * dt;
                    p.Rt.anchoredPosition = p.Pos;
                    float s = Mathf.Lerp(p.Size0, p.Size1, k);
                    p.Rt.sizeDelta = p.Streak ? new Vector2(s * 3.4f, s * .5f) : new Vector2(s, s);
                    p.Rt.localRotation = Quaternion.Euler(0, 0, p.Streak ? Mathf.Atan2(p.Vel.y, p.Vel.x) * Mathf.Rad2Deg : p.Rot + p.Spin * p.Age);
                    Color c = Color.Lerp(p.C0, p.C1, k);
                    c.a *= Mathf.Clamp01(k / .12f) * (1f - Mathf.Clamp01((k - .55f) / .45f));
                    p.Img.color = c;
                }
                if (!any && clock >= hold) break;
                yield return null;
            }
        }

        private RectTransform Bar(RectTransform layer, Vector2 a, Vector2 b, float thick, Color color)
        {
            var rt = ColorBox(layer, "Bar", 0, 0, Mathf.Max(1f, Vector2.Distance(a, b)), thick, color);
            rt.pivot = new Vector2(0f, .5f);
            rt.anchoredPosition = a;
            rt.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(b.y - a.y, b.x - a.x) * Mathf.Rad2Deg);
            var image = rt.GetComponent<Image>();
            if (image) image.raycastTarget = false;
            return rt;
        }

        private IEnumerator Flash(RectTransform layer, Vector2 at, float size, Color color, float duration)
        {
            if (!layer) yield break;
            var flash = Shade(layer, 0, 0, size, size, color);
            flash.rectTransform.pivot = new Vector2(.5f, .5f);
            flash.rectTransform.anchoredPosition = at;
            for (float t = 0; t < duration && flash; t += Time.unscaledDeltaTime)
            {
                float k = t / duration;
                flash.rectTransform.localScale = Vector3.one * (.35f + k * .9f);
                flash.color = new Color(color.r, color.g, color.b, color.a * (1f - k) * (1f - k));
                yield return null;
            }
            if (flash) Destroy(flash.gameObject);
        }

        private IEnumerator RingFx(RectTransform layer, Vector2 at, float from, float to, Color color, float duration, float spin, string texture = null)
        {
            if (!layer) yield break;
            var ring = Picture(layer, texture ?? Foundry + "DropRing", 0, 0, from, from, color);
            ring.raycastTarget = false;
            ring.rectTransform.pivot = new Vector2(.5f, .5f);
            ring.rectTransform.anchoredPosition = at;
            for (float t = 0; t < duration && ring; t += Time.unscaledDeltaTime)
            {
                float k = t / duration, e = 1f - (1f - k) * (1f - k);
                ring.rectTransform.sizeDelta = Vector2.one * Mathf.Lerp(from, to, e);
                ring.rectTransform.localRotation = Quaternion.Euler(0, 0, spin * k);
                ring.color = new Color(color.r, color.g, color.b, color.a * (1f - k * k));
                yield return null;
            }
            if (ring) Destroy(ring.gameObject);
        }

        private IEnumerator FadeBars(List<RectTransform> bars, float delay, float duration)
        {
            yield return new WaitForSecondsRealtime(delay);
            var images = new List<Image>();
            foreach (var bar in bars) if (bar) images.Add(bar.GetComponent<Image>());
            var start = new List<float>();
            foreach (var image in images) start.Add(image ? image.color.a : 0f);
            for (float t = 0; t < duration; t += Time.unscaledDeltaTime)
            {
                float k = t / duration;
                for (int i = 0; i < images.Count; i++)
                    if (images[i]) { var c = images[i].color; c.a = start[i] * (1f - k); images[i].color = c; }
                yield return null;
            }
            foreach (var bar in bars) if (bar) Destroy(bar.gameObject);
        }

        // ---------------------------------------------------------------- spells

        private IEnumerator DelayedSpell(RectTransform layer, Card card, Vector2 from, Vector2 to, float delay)
        {
            if (delay > 0f) yield return new WaitForSecondsRealtime(delay);
            if (!layer || match == null || page != Page.Battle || reduceMotion) yield break;
            // Use the mapped transparent atlas for this exact card when it has been imported.
            // The existing tailored procedural spell remains the safe fallback during art setup.
            if (TryStartCardVfx(layer, card.Id, to, new Vector2(280f, 280f))) yield break;
            string r = card.Rules ?? "";
            bool wide = r.Contains("Choose a lane") || r.Contains("every unit") || r.Contains("all ");
            var kind = PickFx(card);
            var pal = Pal(card.Faction);
            int motif = Motif(card.Id);
            int shots = wide ? 3 : 1;
            for (int i = 0; i < shots; i++)
            {
                Vector2 target = to + new Vector2((i - (shots - 1) / 2f) * (LaneWidth * .3f), 0f);
                StartCoroutine(OneFx(layer, kind, from, target, pal[0], pal[1], motif));
                if (i < shots - 1) yield return new WaitForSecondsRealtime(.09f);
            }
        }

        private IEnumerator OneFx(RectTransform layer, FxKind kind, Vector2 from, Vector2 to, Color main, Color accent, int motif)
        {
            switch (kind)
            {
                case FxKind.Bolt: return BoltFx(layer, to, main, accent, motif);
                case FxKind.Fire: return FireFx(layer, from, to, main, accent);
                case FxKind.Frost: return FrostFx(layer, to, main, accent);
                case FxKind.Shadow: return ShadowFx(layer, to, main, accent);
                case FxKind.Rune: return RuneFx(layer, to, main, accent, motif);
                case FxKind.Heal: return HealFx(layer, to, main, accent);
                case FxKind.Ward: return WardFx(layer, to, main, accent);
                case FxKind.Summon: return SummonFx(layer, to, main, accent);
                case FxKind.Slash: return SlashFx(layer, to, main, accent, motif);
                case FxKind.Quake: return QuakeFx(layer, to);
                case FxKind.Wind: return WindFx(layer, from, to, main, accent);
                case FxKind.Curse: return CurseFx(layer, to, main, accent);
                case FxKind.Draw: return DrawFx(layer, to, main, accent);
                default: return ArrowFx(layer, from, to, main, accent);
            }
        }

        private IEnumerator BoltFx(RectTransform layer, Vector2 to, Color main, Color accent, int motif)
        {
            Vector2 sky = new Vector2(to.x + (motif % 2 == 0 ? -90f : 90f), 20f);
            UiSound("unit-hit");
            for (int pass = 0; pass < 3; pass++)
            {
                var bars = new List<RectTransform>();
                Vector2 prev = sky;
                const int segments = 9;
                for (int i = 1; i <= segments; i++)
                {
                    Vector2 p = Vector2.Lerp(sky, to, i / (float)segments);
                    if (i < segments) p += new Vector2(Random.Range(-46f, 46f), 0f);
                    bars.Add(Bar(layer, prev, p, 34f, new Color(main.r, main.g, main.b, .28f)));
                    bars.Add(Bar(layer, prev, p, 10f, accent));
                    bars.Add(Bar(layer, prev, p, 4f, Color.white));
                    prev = p;
                }
                if (pass == 0)
                {
                    StartCoroutine(Flash(layer, to, 360f, Color.white, .25f));
                    StartCoroutine(RingFx(layer, to, 60f, 330f, main, .4f, 0f));
                    StartCoroutine(Burst(layer, to, accent, 12, 340f));
                    StartCoroutine(Shake(11f));
                }
                yield return new WaitForSecondsRealtime(.07f);
                foreach (var b in bars) if (b) Destroy(b.gameObject);
                yield return new WaitForSecondsRealtime(.03f);
            }
        }

        private IEnumerator FireFx(RectTransform layer, Vector2 from, Vector2 to, Color main, Color accent)
        {
            UiSound("attack-swing-2");
            var list = new List<Spark>();
            StartCoroutine(RunSparks(list, 1.6f));
            var orb = Shade(layer, 0, 0, 96f, 96f, main);
            var core = Shade(layer, 0, 0, 44f, 44f, Color.white);
            orb.rectTransform.pivot = core.rectTransform.pivot = new Vector2(.5f, .5f);
            const float flight = .4f;
            for (float t = 0; t < flight; t += Time.unscaledDeltaTime)
            {
                float k = t / flight;
                Vector2 pos = Vector2.Lerp(from, to, k * k * (3f - 2f * k)) + new Vector2(0f, 90f * Mathf.Sin(k * Mathf.PI));
                orb.rectTransform.anchoredPosition = pos;
                core.rectTransform.anchoredPosition = pos;
                for (int i = 0; i < 2; i++)
                    Emit(list, layer, pos + Random.insideUnitCircle * 14f, Random.insideUnitCircle * 70f + new Vector2(0, 30f), .38f, 34f, 4f, accent, new Color(.6f, .1f, .02f, 0f));
                yield return null;
            }
            if (orb) Destroy(orb.gameObject);
            if (core) Destroy(core.gameObject);
            UiSound("hero-hit");
            StartCoroutine(Flash(layer, to, 400f, accent, .3f));
            StartCoroutine(RingFx(layer, to, 50f, 300f, main, .45f, 0f));
            StartCoroutine(Shake(9f));
            for (int i = 0; i < 24; i++)
            {
                float a = Random.value * Mathf.PI * 2f, speed = Random.Range(120f, 430f);
                Emit(list, layer, to, new Vector2(Mathf.Cos(a), Mathf.Sin(a) + .3f) * speed, Random.Range(.5f, .9f), Random.Range(22f, 42f), 4f, accent, new Color(.7f, .15f, 0f, 0f), -520f);
            }
            for (int i = 0; i < 8; i++)
                Emit(list, layer, to + Random.insideUnitCircle * 30f, new Vector2(Random.Range(-40f, 40f), Random.Range(60f, 160f)), 1.0f, 60f, 150f, new Color(.12f, .09f, .09f, .55f), new Color(.05f, .05f, .05f, 0f), 0f, Random.Range(0f, .15f));
        }

        private IEnumerator FrostFx(RectTransform layer, Vector2 to, Color main, Color accent)
        {
            UiSound("shield-break");
            var list = new List<Spark>();
            StartCoroutine(RunSparks(list, 1.6f));
            StartCoroutine(Flash(layer, to, 380f, accent, .3f));
            StartCoroutine(RingFx(layer, to, 40f, 320f, main, .45f, 40f));
            StartCoroutine(Shake(8f));
            for (int i = 0; i < 16; i++)
            {
                float a = i / 16f * Mathf.PI * 2f + Random.value * .3f, speed = Random.Range(260f, 520f);
                Emit(list, layer, to, new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * speed, Random.Range(.45f, .75f), 34f, 8f, accent, main, 0f, 0f, true);
            }
            for (int i = 0; i < 26; i++)
                Emit(list, layer, to + new Vector2(Random.Range(-190f, 190f), Random.Range(200f, 320f)), new Vector2(Random.Range(-40f, 40f), Random.Range(-260f, -140f)),
                    Random.Range(.7f, 1.2f), Random.Range(8f, 16f), 5f, Color.white, accent, 0f, Random.Range(0f, .3f));
            yield break;
        }

        private IEnumerator ShadowFx(RectTransform layer, Vector2 to, Color main, Color accent)
        {
            UiSound("relic-reveal");
            var list = new List<Spark>();
            StartCoroutine(RunSparks(list, 2f));
            for (int i = 0; i < 22; i++)
            {
                float a = i / 22f * Mathf.PI * 2f;
                Vector2 start = to + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 170f;
                Emit(list, layer, start, (to - start) / .5f, .5f, 26f, 6f, accent, main);
            }
            yield return new WaitForSecondsRealtime(.5f);
            StartCoroutine(Flash(layer, to, 320f, new Color(.04f, .02f, .08f, .7f), .4f));
            StartCoroutine(RingFx(layer, to, 40f, 300f, accent, .5f, -60f));
            StartCoroutine(Shake(6f));
            for (int i = 0; i < 12; i++)
                Emit(list, layer, to + new Vector2(Random.Range(-60f, 60f), Random.Range(-30f, 30f)), new Vector2(Random.Range(-30f, 30f), Random.Range(70f, 190f)),
                    Random.Range(.9f, 1.4f), Random.Range(26f, 48f), 10f, main, new Color(accent.r, accent.g, accent.b, 0f), 20f);
        }

        private IEnumerator RuneFx(RectTransform layer, Vector2 to, Color main, Color accent, int motif)
        {
            UiSound("skill-unlock");
            var list = new List<Spark>();
            StartCoroutine(RunSparks(list, 1.6f));
            StartCoroutine(RingFx(layer, to, 60f, 300f, main, .8f, motif % 2 == 0 ? 120f : -120f));
            StartCoroutine(RingFx(layer, to, 40f, 210f, accent, .7f, motif % 2 == 0 ? -160f : 160f));
            var glyphs = new List<RectTransform>();
            for (int i = 0; i < 8; i++)
            {
                float a = i / 8f * Mathf.PI * 2f;
                Vector2 c = to + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 96f;
                Vector2 d = new Vector2(-Mathf.Sin(a), Mathf.Cos(a)) * (motif % 3 == 0 ? 30f : 18f);
                glyphs.Add(Bar(layer, c - d, c + d, 7f, accent));
                glyphs.Add(Bar(layer, c, c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 26f, 5f, main));
            }
            StartCoroutine(FadeBars(glyphs, .35f, .45f));
            StartCoroutine(Flash(layer, to, 300f, main, .5f));
            for (int i = 0; i < 18; i++)
                Emit(list, layer, to + Random.insideUnitCircle * 110f, new Vector2(Random.Range(-20f, 20f), Random.Range(90f, 240f)), Random.Range(.7f, 1.2f), Random.Range(12f, 24f), 3f, accent, main, 0f, Random.Range(0f, .25f));
            yield break;
        }

        private IEnumerator HealFx(RectTransform layer, Vector2 to, Color main, Color accent)
        {
            UiSound("star-gain");
            var list = new List<Spark>();
            StartCoroutine(RunSparks(list, 1.6f));
            var green = new Color(.55f, 1f, .65f);
            StartCoroutine(Flash(layer, to, 340f, new Color(green.r, green.g, green.b, .8f), .5f));
            StartCoroutine(RingFx(layer, to, 50f, 240f, green, .6f, 0f));
            for (int i = 0; i < 22; i++)
                Emit(list, layer, to + new Vector2(Random.Range(-80f, 80f), Random.Range(-60f, 40f)), new Vector2(Random.Range(-14f, 14f), Random.Range(70f, 210f)),
                    Random.Range(.8f, 1.3f), Random.Range(14f, 28f), 4f, green, Color.Lerp(green, Color.white, .6f), 0f, Random.Range(0f, .35f));
            yield break;
        }

        private IEnumerator WardFx(RectTransform layer, Vector2 to, Color main, Color accent)
        {
            UiSound("shield-gain");
            var list = new List<Spark>();
            StartCoroutine(RunSparks(list, 1.2f));
            var blue = new Color(.6f, .85f, 1f);
            StartCoroutine(Flash(layer, to, 300f, new Color(blue.r, blue.g, blue.b, .7f), .4f));
            StartCoroutine(RingFx(layer, to, 90f, 280f, main, .5f, 0f, Foundry + "UnitTokenGlow"));
            StartCoroutine(RingFx(layer, to, 60f, 200f, blue, .45f, 0f));
            var shield = Picture(layer, Foundry + "Shield", 0, 0, 120f, 120f, Color.white);
            shield.raycastTarget = false;
            shield.rectTransform.pivot = new Vector2(.5f, .5f);
            shield.rectTransform.anchoredPosition = to;
            for (int i = 0; i < 10; i++)
            {
                float a = i / 10f * Mathf.PI * 2f;
                Emit(list, layer, to + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 80f, new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 160f, .6f, 20f, 4f, blue, accent);
            }
            for (float t = 0; t < .6f && shield; t += Time.unscaledDeltaTime)
            {
                float k = t / .6f;
                shield.rectTransform.localScale = Vector3.one * (.5f + .9f * (1f - (1f - k) * (1f - k)));
                shield.color = new Color(1, 1, 1, 1f - k * k);
                yield return null;
            }
            if (shield) Destroy(shield.gameObject);
        }

        private IEnumerator SummonFx(RectTransform layer, Vector2 to, Color main, Color accent)
        {
            UiSound("beacon-claim");
            var list = new List<Spark>();
            StartCoroutine(RunSparks(list, 1.4f));
            StartCoroutine(Pillar(layer, to, main, .5f, 130f));
            StartCoroutine(RingFx(layer, to + new Vector2(0, -70f), 40f, 300f, accent, .55f, 90f));
            StartCoroutine(Flash(layer, to, 300f, accent, .35f));
            StartCoroutine(Shake(5f));
            for (int i = 0; i < 18; i++)
                Emit(list, layer, to + new Vector2(Random.Range(-70f, 70f), -60f), new Vector2(Random.Range(-30f, 30f), Random.Range(120f, 300f)), Random.Range(.6f, 1.1f), Random.Range(12f, 24f), 3f, main, accent, -160f, Random.Range(.1f, .4f));
            yield break;
        }

        private IEnumerator Pillar(RectTransform layer, Vector2 at, Color color, float duration, float width)
        {
            var bar = Bar(layer, new Vector2(at.x, 40f), new Vector2(at.x, at.y - 70f), width, new Color(color.r, color.g, color.b, .5f));
            var core = Bar(layer, new Vector2(at.x, 40f), new Vector2(at.x, at.y - 70f), width * .3f, Color.white);
            var barImage = bar.GetComponent<Image>();
            var coreImage = core.GetComponent<Image>();
            for (float t = 0; t < duration && bar; t += Time.unscaledDeltaTime)
            {
                float k = t / duration;
                float w = width * (1f - k * k);
                bar.sizeDelta = new Vector2(bar.sizeDelta.x, Mathf.Max(2f, w));
                core.sizeDelta = new Vector2(core.sizeDelta.x, Mathf.Max(1f, w * .3f));
                if (barImage) barImage.color = new Color(color.r, color.g, color.b, .5f * (1f - k));
                if (coreImage) coreImage.color = new Color(1, 1, 1, 1f - k);
                yield return null;
            }
            if (bar) Destroy(bar.gameObject);
            if (core) Destroy(core.gameObject);
        }

        private IEnumerator SlashFx(RectTransform layer, Vector2 to, Color main, Color accent, int motif)
        {
            UiSound("attack-swing-1");
            var list = new List<Spark>();
            StartCoroutine(RunSparks(list, 1f));
            float flip = motif % 2 == 0 ? 1f : -1f;
            for (int i = 0; i < 3; i++)
            {
                Vector2 a = to + new Vector2(-150f * flip, 100f - i * 18f), b = to + new Vector2(150f * flip, -100f - i * 18f);
                var bars = new List<RectTransform> { Bar(layer, a, a, 14f, new Color(main.r, main.g, main.b, .6f)), Bar(layer, a, a, 5f, Color.white) };
                for (float t = 0; t < .09f; t += Time.unscaledDeltaTime)
                {
                    Vector2 tip = Vector2.Lerp(a, b, t / .09f);
                    foreach (var bar in bars) { bar.sizeDelta = new Vector2(Vector2.Distance(a, tip), bar.sizeDelta.y); }
                    yield return null;
                }
                foreach (var bar in bars) bar.sizeDelta = new Vector2(Vector2.Distance(a, b), bar.sizeDelta.y);
                StartCoroutine(FadeBars(bars, .04f, .25f));
                if (i == 0) { StartCoroutine(Shake(9f)); StartCoroutine(Flash(layer, to, 280f, accent, .25f)); }
                for (int s = 0; s < 6; s++)
                    Emit(list, layer, to, Random.insideUnitCircle.normalized * Random.Range(200f, 420f), .4f, 26f, 5f, accent, main, -300f, 0f, true);
            }
        }

        private IEnumerator QuakeFx(RectTransform layer, Vector2 to)
        {
            UiSound("shield-break");
            var list = new List<Spark>();
            StartCoroutine(RunSparks(list, 1.6f));
            StartCoroutine(Shake(16f));
            StartCoroutine(Flash(layer, to, 360f, new Color(1f, .85f, .6f, .7f), .25f));
            var cracks = new List<RectTransform>();
            for (int i = 0; i < 7; i++)
            {
                float a = i / 7f * Mathf.PI * 2f + Random.value * .4f;
                Vector2 dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a) * .5f);
                Vector2 mid = to + dir * Random.Range(70f, 100f) + new Vector2(Random.Range(-14f, 14f), Random.Range(-10f, 10f));
                cracks.Add(Bar(layer, to, mid, 8f, new Color(.08f, .05f, .04f, .9f)));
                cracks.Add(Bar(layer, mid, to + dir * Random.Range(150f, 200f), 5f, new Color(.08f, .05f, .04f, .9f)));
            }
            StartCoroutine(FadeBars(cracks, .55f, .45f));
            for (int i = 0; i < 14; i++)
                Emit(list, layer, to + new Vector2(Random.Range(-40f, 40f), -20f), new Vector2(Random.Range(-260f, 260f), Random.Range(280f, 620f)), Random.Range(.6f, 1f), Random.Range(12f, 26f), 8f,
                    new Color(.55f, .42f, .3f), new Color(.3f, .22f, .16f, 0f), -980f);
            for (int i = 0; i < 10; i++)
                Emit(list, layer, to + new Vector2(Random.Range(-80f, 80f), -30f), new Vector2(Random.Range(-160f, 160f), Random.Range(20f, 90f)), 1f, 50f, 150f,
                    new Color(.62f, .55f, .46f, .5f), new Color(.4f, .36f, .3f, 0f));
            yield break;
        }

        private IEnumerator WindFx(RectTransform layer, Vector2 from, Vector2 to, Color main, Color accent)
        {
            UiSound("attack-swing-2");
            var list = new List<Spark>();
            StartCoroutine(RunSparks(list, 1.2f));
            Vector2 dir = (to - from).normalized;
            if (dir.sqrMagnitude < .1f) dir = Vector2.up;
            Vector2 side = new Vector2(-dir.y, dir.x);
            for (int i = 0; i < 14; i++)
            {
                Vector2 start = from + side * Random.Range(-120f, 120f);
                Emit(list, layer, start, dir * Random.Range(1100f, 1700f) + side * Random.Range(-60f, 60f), Random.Range(.25f, .4f), Random.Range(20f, 34f), 6f, Color.Lerp(accent, Color.white, .5f), accent, 0f, Random.Range(0f, .12f), true);
            }
            yield return new WaitForSecondsRealtime(.3f);
            StartCoroutine(Flash(layer, to, 260f, accent, .3f));
            StartCoroutine(RingFx(layer, to, 40f, 220f, main, .4f, 0f));
            for (int i = 0; i < 8; i++)
                Emit(list, layer, to, Random.insideUnitCircle.normalized * Random.Range(80f, 220f), .5f, 22f, 4f, accent, main);
        }

        private IEnumerator CurseFx(RectTransform layer, Vector2 to, Color main, Color accent)
        {
            UiSound("target-denied");
            var list = new List<Spark>();
            StartCoroutine(RunSparks(list, 1.5f));
            var dark = new Color(.55f, .25f, .85f);
            StartCoroutine(Flash(layer, to, 320f, new Color(.1f, .02f, .16f, .75f), .45f));
            StartCoroutine(RingFx(layer, to, 220f, 60f, dark, .6f, -200f));
            StartCoroutine(RingFx(layer, to, 160f, 40f, main, .5f, 220f));
            for (int i = 0; i < 16; i++)
                Emit(list, layer, to + new Vector2(Random.Range(-70f, 70f), Random.Range(90f, 190f)), new Vector2(Random.Range(-14f, 14f), Random.Range(-240f, -130f)), Random.Range(.5f, .9f), Random.Range(14f, 26f), 5f, dark, new Color(.2f, .05f, .3f, 0f), 0f, Random.Range(0f, .35f));
            yield break;
        }

        private IEnumerator DrawFx(RectTransform layer, Vector2 to, Color main, Color accent)
        {
            UiSound("card-draw-1");
            var list = new List<Spark>();
            StartCoroutine(RunSparks(list, 1.4f));
            Vector2 hand = new Vector2(960f, -930f);
            StartCoroutine(Flash(layer, to, 240f, accent, .3f));
            for (int i = 0; i < 12; i++)
            {
                Vector2 start = to + Random.insideUnitCircle * 60f;
                Vector2 v = (hand - start).normalized * Random.Range(1000f, 1400f);
                Emit(list, layer, start, v, Random.Range(.35f, .5f), Random.Range(22f, 34f), 6f, accent, main, 0f, Random.Range(0f, .2f), true);
            }
            yield break;
        }

        private IEnumerator ArrowFx(RectTransform layer, Vector2 from, Vector2 to, Color main, Color accent)
        {
            UiSound("attack-swing-1");
            var list = new List<Spark>();
            StartCoroutine(RunSparks(list, 1f));
            var head = Emit(list, layer, from, (to - from) / .2f, .2f, 40f, 40f, Color.white, accent, 0f, 0f, true);
            head.Age = 0f;
            yield return new WaitForSecondsRealtime(.2f);
            StartCoroutine(Burst(layer, to, accent, 8, 260f));
            StartCoroutine(Shake(5f));
        }

        // ---------------------------------------------------------------- units

        private IEnumerator DelayedEntrance(RectTransform layer, Card card, Vector2 at, float delay)
        {
            if (delay > 0f) yield return new WaitForSecondsRealtime(delay);
            if (!layer || match == null || page != Page.Battle || reduceMotion) yield break;
            float width = card.Kind == Kind.Site ? 120f : 240f;
            if (TryStartCardVfx(layer, card.Id, at, Vector2.one * width)) yield break;
            StartCoroutine(UnitEntranceFx(layer, card, at));
        }

        /// <summary>Faction-coloured arrival, varied per card, with extra staging for Guard, Rush and Mythic cards.</summary>
        private IEnumerator UnitEntranceFx(RectTransform layer, Card card, Vector2 at)
        {
            if (!layer || reduceMotion) yield break;
            var pal = Pal(card.Faction);
            int motif = Motif(card.Id);
            float spin = motif % 2 == 0 ? 90f : -90f;
            var list = new List<Spark>();
            StartCoroutine(RunSparks(list, 1.6f));
            StartCoroutine(RingFx(layer, at + new Vector2(0, -80f), 40f, card.Guard ? 430f : 300f, pal[0], .5f, spin));
            StartCoroutine(Flash(layer, at, 300f, pal[1], .3f));
            StartCoroutine(Pillar(layer, at, card.Legendary ? new Color(1f, .85f, .4f) : pal[0], card.Legendary ? .8f : .4f, card.Legendary ? 190f : 90f));
            int count = 10 + motif % 8;
            for (int i = 0; i < count; i++)
            {
                float a = i / (float)count * Mathf.PI * 2f + motif * .1f;
                float speed = 140f + (motif % 5) * 30f + Random.value * 90f;
                Emit(list, layer, at + new Vector2(0, -60f), new Vector2(Mathf.Cos(a) * speed, Mathf.Sin(a) * speed * .6f + 120f), Random.Range(.5f, .9f), Random.Range(14f, 28f), 3f, pal[0], pal[1], -260f);
            }
            if (card.Guard)
            {
                UiSound("card-play-site");
                StartCoroutine(Shake(7f));
                for (int i = 0; i < 10; i++)
                    Emit(list, layer, at + new Vector2(Random.Range(-60f, 60f), -80f), new Vector2((i % 2 == 0 ? 1f : -1f) * Random.Range(120f, 260f), Random.Range(10f, 60f)), 1f, 46f, 140f,
                        new Color(.62f, .55f, .46f, .5f), new Color(.4f, .36f, .3f, 0f));
            }
            if (card.Rush)
            {
                float side = motif % 2 == 0 ? -1f : 1f;
                for (int i = 0; i < 9; i++)
                    Emit(list, layer, at + new Vector2(side * 420f, Random.Range(-70f, 70f)), new Vector2(-side * Random.Range(1300f, 1900f), 0f), .3f, Random.Range(22f, 36f), 6f, Color.white, pal[0], 0f, Random.Range(0f, .1f), true);
            }
            if (card.Legendary)
            {
                StartCoroutine(Shake(10f));
                StartCoroutine(LegendBanner(layer, card, pal[0]));
            }
        }

        private IEnumerator LegendBanner(RectTransform layer, Card card, Color tint)
        {
            var holder = Box(layer, "Legend", 0, 380, 1920, 120);
            var group = holder.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            ColorBox(holder, "Band", 0, 0, 1920, 120, new Color(0, 0, 0, .62f));
            ColorBox(holder, "Top rule", 0, 0, 1920, 3, new Color(Gold.r, Gold.g, Gold.b, .9f));
            ColorBox(holder, "Bottom rule", 0, 117, 1920, 3, new Color(Gold.r, Gold.g, Gold.b, .9f));
            OutlinedText(holder, card.Name, 0, 8, 1920, 78, 64, Gold, true, TextAnchor.MiddleCenter, 30);
            SafeText(holder, FactionName(card.Faction) + " Mythic", 0, 84, 1920, 30, 22, tint, false, TextAnchor.MiddleCenter, 14);
            const float duration = 1.5f;
            for (float t = 0; t < duration && holder; t += Time.unscaledDeltaTime)
            {
                float k = t / duration;
                float slide = 1f - Mathf.Clamp01(k / .18f);
                holder.anchoredPosition = new Vector2(-300f * slide * slide + (k > .85f ? (k - .85f) * 900f : 0f), -380f);
                group.alpha = k < .12f ? k / .12f : k > .8f ? 1f - (k - .8f) / .2f : 1f;
                yield return null;
            }
            if (holder) Destroy(holder.gameObject);
        }

        // ---------------------------------------------------------------- prophecies

        private IEnumerator ProphecyFx(RectTransform layer, string id)
        {
            if (!layer || reduceMotion) yield break;
            UiSound("prophecy-reveal");
            var list = new List<Spark>();
            StartCoroutine(RunSparks(list, 2.6f));
            switch (id)
            {
                case "P01": // Fimbulwinter: a white-blue freeze sweeps the table while snow drives across
                    StartCoroutine(ScreenWash(layer, new Color(.7f, .9f, 1f), .38f, 1.6f));
                    StartCoroutine(Shake(8f));
                    for (int i = 0; i < 80; i++)
                        Emit(list, layer, new Vector2(Random.Range(-100f, 1900f), Random.Range(0f, 80f)), new Vector2(Random.Range(-260f, -120f), Random.Range(-520f, -300f)), Random.Range(1.2f, 2f), Random.Range(10f, 22f), 6f, Color.white, new Color(.7f, .9f, 1f, 0f), 0f, Random.Range(0f, 1f));
                    break;
                case "P02": // The Sun Goes Out: the table darkens around a golden corona
                    StartCoroutine(ScreenWash(layer, new Color(0, 0, .02f), .78f, 2.2f));
                    StartCoroutine(Flash(layer, new Vector2(960f, -300f), 360f, Color.black, 1.8f));
                    StartCoroutine(RingFx(layer, new Vector2(960f, -300f), 380f, 240f, new Color(1f, .78f, .3f), 1.8f, 0f));
                    StartCoroutine(RingFx(layer, new Vector2(960f, -300f), 300f, 200f, new Color(1f, .9f, .6f), 1.6f, 0f));
                    break;
                case "P03": // The Sea Walks Inland: three bands of water sweep across
                    for (int i = 0; i < 3; i++) StartCoroutine(WaveBand(layer, -300f - i * 170f, i * .25f));
                    StartCoroutine(Shake(7f));
                    break;
                case "P04": // The Dead Knock: three green rings, one knock at a time
                    for (int i = 0; i < 3; i++)
                    {
                        float delay = i * .45f;
                        StartCoroutine(DelayedRing(layer, new Vector2(960f, -540f), 80f, 760f, new Color(.5f, 1f, .7f), .7f, delay));
                        StartCoroutine(DelayedShake(5f, delay));
                    }
                    for (int i = 0; i < 24; i++)
                        Emit(list, layer, new Vector2(Random.Range(320f, 1600f), Random.Range(-700f, -420f)), new Vector2(Random.Range(-20f, 20f), Random.Range(60f, 160f)), Random.Range(1f, 1.6f), Random.Range(30f, 56f), 12f, new Color(.5f, 1f, .7f, .8f), new Color(.4f, .3f, .8f, 0f), 0f, Random.Range(0f, 1.2f));
                    break;
                case "P05": // Bifröst Breaks: rainbow beams slam into the board
                    for (int i = 0; i < 6; i++)
                        StartCoroutine(DelayedPillar(layer, new Vector2(400f + i * 220f, -560f), Color.HSVToRGB(i / 6f, .65f, 1f), .5f, 120f, i * .12f));
                    StartCoroutine(DelayedShake(14f, .3f));
                    StartCoroutine(ScreenWash(layer, Color.white, .5f, .6f));
                    break;
                default: // P06 The Gates Burn: the whole table catches fire
                    StartCoroutine(ScreenWash(layer, new Color(1f, .45f, .1f), .4f, 2f));
                    StartCoroutine(Shake(10f));
                    for (int i = 0; i < 70; i++)
                        Emit(list, layer, new Vector2(Random.Range(0f, 1920f), -1080f + Random.Range(0f, 60f)), new Vector2(Random.Range(-60f, 60f), Random.Range(200f, 520f)), Random.Range(1f, 1.8f), Random.Range(20f, 46f), 6f, new Color(1f, .7f, .2f, .9f), new Color(.7f, .1f, 0f, 0f), 60f, Random.Range(0f, 1f));
                    break;
            }
            yield break;
        }

        private IEnumerator ScreenWash(RectTransform layer, Color color, float peak, float duration)
        {
            var wash = ColorBox(layer, "Wash", 0, 0, 1920, 1080, new Color(color.r, color.g, color.b, 0f));
            var image = wash.GetComponent<Image>();
            if (image) image.raycastTarget = false;
            for (float t = 0; t < duration && wash; t += Time.unscaledDeltaTime)
            {
                float k = t / duration;
                float a = k < .25f ? k / .25f : 1f - (k - .25f) / .75f;
                if (image) image.color = new Color(color.r, color.g, color.b, peak * a);
                yield return null;
            }
            if (wash) Destroy(wash.gameObject);
        }

        private IEnumerator WaveBand(RectTransform layer, float y, float delay)
        {
            yield return new WaitForSecondsRealtime(delay);
            var band = ColorBox(layer, "Wave", -1920, 0, 1920, 130, new Color(.35f, .7f, .9f, .4f));
            band.anchoredPosition = new Vector2(-1920f, y);
            var foam = ColorBox(band, "Foam", 0, 0, 1920, 10, new Color(1f, 1f, 1f, .7f));
            foreach (var image in band.GetComponentsInChildren<Image>()) image.raycastTarget = false;
            const float duration = 1.1f;
            for (float t = 0; t < duration && band; t += Time.unscaledDeltaTime)
            {
                float k = t / duration;
                band.anchoredPosition = new Vector2(Mathf.Lerp(-1920f, 1920f, k), y + Mathf.Sin(k * 14f) * 12f);
                yield return null;
            }
            if (band) Destroy(band.gameObject);
        }

        private IEnumerator DelayedRing(RectTransform layer, Vector2 at, float from, float to, Color color, float duration, float delay)
        {
            yield return new WaitForSecondsRealtime(delay);
            yield return RingFx(layer, at, from, to, color, duration, 0f);
        }

        private IEnumerator DelayedShake(float magnitude, float delay)
        {
            yield return new WaitForSecondsRealtime(delay);
            StartCoroutine(Shake(magnitude));
        }

        private IEnumerator DelayedPillar(RectTransform layer, Vector2 at, Color color, float duration, float width, float delay)
        {
            yield return new WaitForSecondsRealtime(delay);
            StartCoroutine(Flash(layer, at, 260f, color, .3f));
            yield return Pillar(layer, at, color, duration, width);
        }
    }
}
