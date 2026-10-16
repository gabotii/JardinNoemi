#!/usr/bin/env python3
"""
Genera las ilustraciones SVG de muestra de JardinNoemi (productos, categorías, hero y about).

Son placeholders con identidad de marca: cuando existan fotografías reales basta con
reemplazar las URLs de imagen en Data/Seed/catalog.json (o subirlas desde el panel admin).

Uso:  python tools/generate_art.py
"""
from __future__ import annotations

import math
import random
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
OUT = ROOT / "wwwroot" / "images"

# (fondo, "sol" decorativo)
BG = {
    "sage": ("#E6EDE1", "#D4E1CD"),
    "sand": ("#F4EDE1", "#E9DCC6"),
    "blush": ("#F7E6DC", "#EFD2C2"),
    "mist": ("#E4ECEA", "#D0DFDC"),
    "cream": ("#FBF6EC", "#F1E6D1"),
    "lilac": ("#EEE8F1", "#DDD3E7"),
    "forest": ("#DCE6D6", "#C5D6BD"),
}

# (principal, sombra, brillo)
POTS = {
    "terracotta": ("#C8703F", "#A9572C", "#DE9368"),
    "cream": ("#EFE6D6", "#D6C8B0", "#FAF5EC"),
    "charcoal": ("#3B413D", "#2A2F2C", "#58605A"),
    "sage": ("#9DB59A", "#7F9C7C", "#B9CDB6"),
    "white": ("#F8F6F1", "#DFDBD1", "#FFFFFF"),
    "blush": ("#E6B8A2", "#D09A80", "#F2CDBB"),
    "cement": ("#BAB6AE", "#9E9990", "#D0CCC5"),
    "sand": ("#D9C3A0", "#C2A67E", "#E8D6B8"),
}

G = {
    "dark": "#2E5A39",
    "mid": "#3D7747",
    "light": "#5A955C",
    "pale": "#8AB97C",
    "lime": "#B7CE6F",
    "stem": "#4C7B4D",
    "vein": "#A6CB96",
    "blue": "#7FA79A",
}

SOIL = "#4A3528"
SHADOW = "#1F3D2B"

_uid = [0]


def uid(prefix: str = "c") -> str:
    _uid[0] += 1
    return f"{prefix}{_uid[0]}"


def f(v: float) -> str:
    s = f"{v:.1f}"
    return s[:-2] if s.endswith(".0") else s


def svg(content: str, viewbox: str = "0 0 800 800", title: str = "", w: int = 800, h: int = 800) -> str:
    t = f"<title>{title}</title>" if title else ""
    return (
        f'<svg xmlns="http://www.w3.org/2000/svg" viewBox="{viewbox}" width="{w}" height="{h}" '
        f'role="img">{t}{content}</svg>\n'
    )


# ---------------------------------------------------------------------------
# Macetas
# ---------------------------------------------------------------------------

def soil(cx=400, top=520, w=210):
    return f'<ellipse cx="{f(cx)}" cy="{f(top + 4)}" rx="{f(w / 2 + 2)}" ry="13" fill="{SOIL}"/>'


def pot(kind="tapered", color="terracotta", cx=400, top=520, w=210, h=165, pattern=None, inner=False):
    main, shade, light = POTS[color]
    out = []
    cid = uid("pc")
    if kind == "tapered":
        rim_h = 36
        rw = w + 26
        bt = top + rim_h
        bw = w * 0.74
        bottom = top + h
        body = (
            f"M{f(cx - w / 2)},{f(bt)} H{f(cx + w / 2)} L{f(cx + bw / 2)},{f(bottom - 12)} "
            f"Q{f(cx + bw / 2)},{f(bottom)} {f(cx + bw / 2 - 12)},{f(bottom)} H{f(cx - bw / 2 + 12)} "
            f"Q{f(cx - bw / 2)},{f(bottom)} {f(cx - bw / 2)},{f(bottom - 12)} Z"
        )
        out.append(f'<clipPath id="{cid}"><path d="{body}"/></clipPath>')
        out.append(f'<path d="{body}" fill="{main}"/>')
        out.append(_pattern(pattern, cid, cx, bt, w, h - rim_h, main, shade, light))
        out.append(
            f'<g clip-path="url(#{cid})"><path d="M{f(cx + w * 0.16)},{f(bt)} H{f(cx + w)} V{f(bottom)} H{f(cx + bw * 0.1)} Z" '
            f'fill="{shade}" opacity=".55"/>'
            f'<rect x="{f(cx - w)}" y="{f(bt)}" width="{f(w * 2)}" height="12" fill="#000" opacity=".13"/></g>'
        )
        out.append(f'<rect x="{f(cx - rw / 2)}" y="{f(top)}" width="{f(rw)}" height="{rim_h}" rx="9" fill="{main}"/>')
        out.append(
            f'<rect x="{f(cx + rw * 0.18)}" y="{f(top)}" width="{f(rw * 0.32)}" height="{rim_h}" rx="9" fill="{shade}" opacity=".45"/>'
        )
        out.append(
            f'<rect x="{f(cx - rw / 2 + 14)}" y="{f(top + 8)}" width="{f(rw * 0.3)}" height="6" rx="3" fill="{light}" opacity=".8"/>'
        )
        if inner:
            out.insert(0, f'<ellipse cx="{f(cx)}" cy="{f(top + 3)}" rx="{f(rw / 2 - 4)}" ry="14" fill="{shade}"/>')
            out.insert(1, f'<ellipse cx="{f(cx)}" cy="{f(top + 6)}" rx="{f(rw / 2 - 16)}" ry="9" fill="#2B211A" opacity=".55"/>')
    elif kind == "bowl":
        rim_h = 22
        body = (
            f"M{f(cx - w / 2)},{f(top + rim_h)} H{f(cx + w / 2)} C{f(cx + w / 2)},{f(top + h * 0.86)} "
            f"{f(cx + w * 0.26)},{f(top + h)} {f(cx)},{f(top + h)} C{f(cx - w * 0.26)},{f(top + h)} "
            f"{f(cx - w / 2)},{f(top + h * 0.86)} {f(cx - w / 2)},{f(top + rim_h)} Z"
        )
        out.append(f'<clipPath id="{cid}"><path d="{body}"/></clipPath>')
        out.append(f'<path d="{body}" fill="{main}"/>')
        out.append(_pattern(pattern, cid, cx, top + rim_h, w, h - rim_h, main, shade, light))
        out.append(
            f'<g clip-path="url(#{cid})"><path d="M{f(cx + w * 0.12)},{f(top)} H{f(cx + w)} V{f(top + h)} H{f(cx + w * 0.02)} Z" '
            f'fill="{shade}" opacity=".5"/><rect x="{f(cx - w)}" y="{f(top + rim_h)}" width="{f(w * 2)}" height="10" fill="#000" opacity=".12"/></g>'
        )
        out.append(f'<rect x="{f(cx - w / 2 - 9)}" y="{f(top)}" width="{f(w + 18)}" height="{rim_h}" rx="11" fill="{main}"/>')
        out.append(
            f'<rect x="{f(cx - w / 2 + 6)}" y="{f(top + 6)}" width="{f(w * 0.28)}" height="5" rx="2.5" fill="{light}" opacity=".85"/>'
        )
    elif kind == "cylinder":
        out.append(f'<clipPath id="{cid}"><rect x="{f(cx - w / 2)}" y="{f(top)}" width="{f(w)}" height="{f(h)}" rx="16"/></clipPath>')
        out.append(f'<rect x="{f(cx - w / 2)}" y="{f(top)}" width="{f(w)}" height="{f(h)}" rx="16" fill="{main}"/>')
        out.append(_pattern(pattern, cid, cx, top, w, h, main, shade, light))
        out.append(
            f'<g clip-path="url(#{cid})"><rect x="{f(cx + w * 0.18)}" y="{f(top)}" width="{f(w)}" height="{f(h)}" fill="{shade}" opacity=".5"/>'
            f'<rect x="{f(cx - w / 2)}" y="{f(top)}" width="{f(w)}" height="14" fill="{light}" opacity=".55"/></g>'
        )
    return "".join(out)


def _pattern(pattern, cid, cx, top, w, h, main, shade, light):
    if not pattern:
        return ""
    rnd = random.Random(cid)
    p = [f'<g clip-path="url(#{cid})">']
    if pattern == "band":
        p.append(f'<rect x="{f(cx - w)}" y="{f(top + h * 0.28)}" width="{f(w * 2)}" height="{f(h * 0.16)}" fill="{light}" opacity=".7"/>')
        p.append(f'<rect x="{f(cx - w)}" y="{f(top + h * 0.5)}" width="{f(w * 2)}" height="5" fill="{shade}" opacity=".6"/>')
    elif pattern == "dots":
        for _ in range(70):
            x = cx - w / 2 + rnd.random() * w
            y = top + rnd.random() * h
            p.append(f'<circle cx="{f(x)}" cy="{f(y)}" r="{f(1.6 + rnd.random() * 1.8)}" fill="{shade}" opacity=".7"/>')
    elif pattern == "stripes":
        x = cx - w / 2 + 12
        while x < cx + w / 2:
            p.append(f'<rect x="{f(x)}" y="{f(top)}" width="5" height="{f(h)}" fill="{light}" opacity=".55"/>')
            x += 22
    elif pattern == "arches":
        for i in range(-3, 4):
            x = cx + i * 46
            p.append(
                f'<path d="M{f(x - 16)},{f(top + h)} V{f(top + h * 0.45)} A16,16 0 0 1 {f(x + 16)},{f(top + h * 0.45)} V{f(top + h)}" '
                f'fill="none" stroke="{light}" stroke-width="5" opacity=".75"/>'
            )
    p.append("</g>")
    return "".join(p)


# ---------------------------------------------------------------------------
# Plantas
# ---------------------------------------------------------------------------

LEAF_MONSTERA = "M0,0 C-30,-8 -98,-4 -108,-82 C-118,-162 -44,-204 0,-206 C44,-204 118,-162 108,-82 C98,-4 30,-8 0,0Z"
LEAF_MONSTERA_R = "M0,0 C30,-8 98,-4 108,-82 C118,-162 44,-204 0,-206Z"


def monstera_leaf(x, y, a, s, color, hole):
    cid = uid("ml")
    slits = "".join(
        f'<path d="M-24,{yy} L-128,{yy - 26}"/><path d="M24,{yy} L128,{yy - 26}"/>' for yy in (-46, -92, -138)
    )
    holes = (
        '<ellipse cx="-52" cy="-120" rx="6" ry="11" transform="rotate(-35 -52 -120)"/>'
        '<ellipse cx="54" cy="-72" rx="6" ry="11" transform="rotate(35 54 -72)"/>'
    )
    return (
        f'<g transform="translate({f(x)} {f(y)}) rotate({f(a)}) scale({f(s)})">'
        f'<clipPath id="{cid}"><path d="{LEAF_MONSTERA}"/></clipPath>'
        f'<path d="{LEAF_MONSTERA}" fill="{color}"/>'
        f'<path d="{LEAF_MONSTERA_R}" fill="#000" opacity=".13"/>'
        f'<path d="M0,-4 C-2,-70 -2,-140 0,-196" stroke="{G["vein"]}" stroke-width="4" fill="none" opacity=".75" stroke-linecap="round"/>'
        f'<g clip-path="url(#{cid})" stroke="{hole}" fill="{hole}" stroke-width="9" stroke-linecap="round">{slits}</g>'
        "</g>"
    )


def plant_monstera(hole, top=520):
    leaves = [
        (300, 330, -38, 1.0, G["dark"]),
        (478, 298, 24, 1.08, G["mid"]),
        (392, 246, -4, 1.02, G["dark"]),
        (262, 446, -72, 0.82, G["mid"]),
        (544, 432, 68, 0.86, G["dark"]),
        (424, 408, 14, 0.64, G["light"]),
    ]
    back = [soil(top=top)]
    for x, y, a, s, c in leaves:
        sx = 400 + (x - 400) * 0.12
        back.append(
            f'<path d="M{f(sx)},{top + 10} Q{f(400 + (x - 400) * 0.35)},{f((y + top) / 2 + 40)} {f(x)},{f(y)}" '
            f'stroke="{G["stem"]}" stroke-width="7" fill="none" stroke-linecap="round"/>'
        )
    for x, y, a, s, c in leaves:
        back.append(monstera_leaf(x, y, a, s, c, hole))
    return "".join(back), ""


def plant_snake(top=520):
    leaves = [
        (-92, 222, -70, 28), (86, 242, 62, 28), (-58, 300, -40, 34), (56, 330, 36, 36),
        (-24, 382, -12, 38), (18, 420, 12, 40), (-4, 256, -28, 30),
    ]
    out = [soil(top=top)]
    for i, (dx, h, lean, w) in enumerate(leaves):
        bx = 400 + dx
        tx = bx + lean
        b = top + 12
        cid = uid("sn")
        d = (
            f"M{f(bx - w / 2)},{b} C{f(bx - w / 2)},{f(b - h * 0.5)} {f(tx - w * 0.36)},{f(b - h * 0.8)} {f(tx)},{f(b - h)} "
            f"C{f(tx + w * 0.36)},{f(b - h * 0.8)} {f(bx + w / 2)},{f(b - h * 0.5)} {f(bx + w / 2)},{b} Z"
        )
        bands = "".join(
            f'<path d="M{f(bx - 60)},{f(yy)} q15,-8 30,0 t30,0 t30,0 t30,0" stroke="#7DA877" stroke-width="5" fill="none" opacity=".55"/>'
            for yy in range(int(b - 20), int(b - h), -26)
        )
        color = G["dark"] if i % 2 else "#35643F"
        out.append(
            f'<clipPath id="{cid}"><path d="{d}"/></clipPath><path d="{d}" fill="{color}"/>'
            f'<g clip-path="url(#{cid})">{bands}<path d="M{f(tx)},{f(b - h)} V{b + 10} H{f(bx + w)} V{f(b - h)}Z" fill="#000" opacity=".1"/></g>'
            f'<path d="{d}" fill="none" stroke="#CFC56A" stroke-width="4"/>'
        )
    return "".join(out), ""


def bezier(p0, p1, p2, p3, t):
    u = 1 - t
    x = u**3 * p0[0] + 3 * u * u * t * p1[0] + 3 * u * t * t * p2[0] + t**3 * p3[0]
    y = u**3 * p0[1] + 3 * u * u * t * p1[1] + 3 * u * t * t * p2[1] + t**3 * p3[1]
    dx = 3 * u * u * (p1[0] - p0[0]) + 6 * u * t * (p2[0] - p1[0]) + 3 * t * t * (p3[0] - p2[0])
    dy = 3 * u * u * (p1[1] - p0[1]) + 6 * u * t * (p2[1] - p1[1]) + 3 * t * t * (p3[1] - p2[1])
    return x, y, math.degrees(math.atan2(dy, dx))


LEAF_HEART = "M0,0 C-10,4 -30,-2 -32,-22 C-34,-46 -14,-60 0,-74 C14,-60 34,-46 32,-22 C30,-2 10,4 0,0Z"


def heart_leaf(x, y, a, s, color, variegated=True):
    streak = (
        '<path d="M0,-8 C-6,-26 -14,-42 -8,-60" stroke="#E4E09A" stroke-width="6" fill="none" opacity=".85" stroke-linecap="round"/>'
        if variegated
        else ""
    )
    return (
        f'<g transform="translate({f(x)} {f(y)}) rotate({f(a)}) scale({f(s)})">'
        f'<path d="{LEAF_HEART}" fill="{color}"/>{streak}'
        f'<path d="M0,-2 V-66" stroke="#000" stroke-width="2" opacity=".15"/></g>'
    )


def trailing_vines(vines, top, seed, colors=None, variegated=True, scale=1.0):
    rnd = random.Random(seed)
    colors = colors or [G["mid"], G["light"], G["dark"]]
    out = []
    for p0, p1, p2, p3 in vines:
        out.append(
            f'<path d="M{f(p0[0])},{f(p0[1])} C{f(p1[0])},{f(p1[1])} {f(p2[0])},{f(p2[1])} {f(p3[0])},{f(p3[1])}" '
            f'stroke="{G["stem"]}" stroke-width="4" fill="none" stroke-linecap="round"/>'
        )
        steps = 7
        for i in range(1, steps + 1):
            t = i / steps
            x, y, ang = bezier(p0, p1, p2, p3, t)
            side = 1 if i % 2 else -1
            a = ang + 90 + side * 55
            s = (0.8 + 0.25 * (1 - t)) * scale
            out.append(heart_leaf(x, y, a, s, rnd.choice(colors), variegated))
    return "".join(out)


def plant_pothos(top=520):
    back = [soil(top=top)]
    rnd = random.Random(7)
    for i in range(9):
        x = 330 + i * 18 + rnd.uniform(-6, 6)
        a = -60 + i * 15 + rnd.uniform(-8, 8)
        back.append(
            f'<path d="M{f(x)},{top + 8} Q{f(x + a * 0.6)},{top - 40} {f(x + a * 1.1)},{top - 70 - rnd.uniform(0, 40)}" '
            f'stroke="{G["stem"]}" stroke-width="4" fill="none"/>'
        )
        bx = x + a * 1.1
        by = top - 70 - rnd.uniform(0, 40)
        back.append(heart_leaf(bx, by + 6, a * 0.8, 1.15, rnd.choice([G["mid"], G["light"], G["dark"]])))
    vines = [
        ((318, top + 18), (250, top + 30), (225, top + 120), (240, top + 245)),
        ((480, top + 18), (560, top + 30), (590, top + 130), (566, top + 262)),
        ((350, top + 20), (300, top + 90), (320, top + 170), (292, top + 232)),
    ]
    front = trailing_vines(vines, top, 11)
    return "".join(back), front


LEAF_FIDDLE = "M0,0 C-14,-10 -44,-40 -50,-92 C-56,-144 -32,-176 0,-178 C32,-176 56,-144 50,-92 C44,-40 14,-10 0,0Z"


def plant_fiddle(top=520):
    out = [soil(top=top)]
    out.append(f'<path d="M400,{top + 10} C404,{top - 120} 396,{top - 230} 404,{top - 350}" stroke="#6B5040" stroke-width="12" fill="none" stroke-linecap="round"/>')
    leaves = [
        (400, top - 40, -62, 0.8, G["mid"]), (402, top - 70, 58, 0.85, G["dark"]),
        (400, top - 120, -48, 0.95, G["dark"]), (401, top - 160, 46, 0.95, G["mid"]),
        (399, top - 210, -36, 0.9, G["mid"]), (402, top - 250, 34, 0.88, G["dark"]),
        (402, top - 300, -18, 0.8, G["light"]), (404, top - 340, 8, 0.78, G["mid"]),
    ]
    for x, y, a, s, c in leaves:
        veins = "".join(
            f'<path d="M0,{yy} L-34,{yy - 22} M0,{yy} L34,{yy - 22}" stroke="{G["vein"]}" stroke-width="2.5" opacity=".6"/>'
            for yy in (-50, -90, -130)
        )
        out.append(
            f'<g transform="translate({f(x)} {f(y)}) rotate({f(a)}) scale({f(s)})"><path d="{LEAF_FIDDLE}" fill="{c}"/>'
            f'<path d="M0,0 C14,-10 44,-40 50,-92 C56,-144 32,-176 0,-178Z" fill="#000" opacity=".12"/>'
            f'<path d="M0,-2 V-170" stroke="{G["vein"]}" stroke-width="4" opacity=".8"/>{veins}</g>'
        )
    return "".join(out), ""


LEAF_LANCE = "M0,0 C-22,-40 -28,-112 0,-196 C28,-112 22,-40 0,0Z"


def plant_peace_lily(top=520):
    out = [soil(top=top)]
    for a, s, c in [(-62, 0.95, G["mid"]), (60, 0.95, G["dark"]), (-38, 1.1, G["dark"]), (36, 1.1, G["mid"]),
                    (-14, 1.2, G["mid"]), (12, 1.15, G["dark"]), (-80, 0.8, G["light"]), (82, 0.8, G["light"])]:
        out.append(
            f'<g transform="translate(400 {top + 8}) rotate({a}) scale({s})"><path d="{LEAF_LANCE}" fill="{c}"/>'
            f'<path d="M0,0 C22,-40 28,-112 0,-196Z" fill="#000" opacity=".12"/><path d="M0,-4 V-186" stroke="{G["vein"]}" stroke-width="3" opacity=".7"/></g>'
        )
    for x, y in [(350, top - 250), (452, top - 225), (400, top - 290)]:
        out.append(f'<path d="M400,{top + 6} Q{f((x + 400) / 2)},{f(y + 120)} {f(x)},{f(y + 40)}" stroke="{G["stem"]}" stroke-width="5" fill="none"/>')
        out.append(
            f'<g transform="translate({f(x)} {f(y + 40)})"><path d="M0,0 C-26,-22 -28,-70 0,-104 C28,-70 26,-22 0,0Z" fill="#FBFAF4"/>'
            f'<path d="M0,0 C26,-22 28,-70 0,-104Z" fill="#E6E4D8"/><rect x="-5" y="-60" width="10" height="40" rx="5" fill="#E9D98A"/></g>'
        )
    return "".join(out), ""


def fern(cx, base_y, fronds, seed, scale=1.0, color_set=None):
    rnd = random.Random(seed)
    colors = color_set or [G["mid"], G["light"], G["dark"], G["pale"]]
    out = []
    for ang, length in fronds:
        a = math.radians(ang)
        L = length * scale
        p0 = (cx, base_y)
        p1 = (cx + math.sin(a) * L * 0.4, base_y - math.cos(a) * L * 0.55)
        droop = 1 if ang >= 0 else -1
        p2 = (cx + math.sin(a) * L * 0.85 + droop * L * 0.15, base_y - math.cos(a) * L * 0.75)
        p3 = (cx + math.sin(a) * L * 1.05 + droop * L * 0.28, base_y - math.cos(a) * L * 0.45)
        color = rnd.choice(colors)
        out.append(
            f'<path d="M{f(p0[0])},{f(p0[1])} C{f(p1[0])},{f(p1[1])} {f(p2[0])},{f(p2[1])} {f(p3[0])},{f(p3[1])}" '
            f'stroke="{color}" stroke-width="{f(3.5 * scale)}" fill="none" stroke-linecap="round"/>'
        )
        n = 16
        for i in range(1, n):
            t = i / n
            x, y, tang = bezier(p0, p1, p2, p3, t)
            size = (1 - t * 0.75) * 17 * scale
            for side in (-1, 1):
                out.append(
                    f'<ellipse cx="{f(x)}" cy="{f(y)}" rx="{f(size)}" ry="{f(size * 0.34)}" fill="{color}" '
                    f'transform="rotate({f(tang + side * 62)} {f(x)} {f(y)}) translate({f(side * size * 0.9)} 0)"/>'
                )
    return "".join(out)


def plant_fern(top=520):
    fronds = [(-75, 250), (75, 250), (-55, 270), (55, 275), (-32, 300), (30, 300), (-10, 310), (12, 290), (-88, 200), (88, 210)]
    return soil(top=top) + fern(400, top + 8, fronds, 5), ""


def plant_pilea(top=520):
    rnd = random.Random(3)
    out = [soil(top=top)]
    out.append(f'<path d="M400,{top + 8} V{top - 60}" stroke="#6E8E4E" stroke-width="9" stroke-linecap="round"/>')
    leaves = []
    for i in range(16):
        a = -80 + i * (160 / 15) + rnd.uniform(-6, 6)
        L = rnd.uniform(110, 190)
        sy = top - rnd.uniform(10, 70)
        x = 400 + math.sin(math.radians(a)) * L
        y = sy - math.cos(math.radians(a)) * L * 0.9
        leaves.append((a, sy, x, y, rnd.uniform(34, 50), rnd.choice([G["mid"], G["light"], "#4F8F4A"])))
    for a, sy, x, y, r, c in leaves:
        out.append(f'<path d="M400,{f(sy)} Q{f((400 + x) / 2)},{f(y - 20)} {f(x)},{f(y)}" stroke="#7FA35F" stroke-width="3.5" fill="none"/>')
    for a, sy, x, y, r, c in sorted(leaves, key=lambda l: l[3]):
        out.append(
            f'<ellipse cx="{f(x)}" cy="{f(y)}" rx="{f(r)}" ry="{f(r * 0.86)}" fill="{c}" transform="rotate({f(a)} {f(x)} {f(y)})"/>'
            f'<ellipse cx="{f(x + r * 0.25)}" cy="{f(y + r * 0.2)}" rx="{f(r * 0.7)}" ry="{f(r * 0.5)}" fill="#000" opacity=".08"/>'
            f'<circle cx="{f(x)}" cy="{f(y)}" r="3.5" fill="{G["vein"]}"/>'
        )
    return "".join(out), ""


def plant_zz(top=520):
    rnd = random.Random(21)
    out = [soil(top=top)]
    stems = [(-50, 300), (-22, 360), (8, 390), (36, 340), (64, 280), (-78, 240)]
    for i, (dx, h) in enumerate(stems):
        x0, y0 = 400 + dx * 0.4, top + 8
        x3, y3 = 400 + dx * 2.2, top - h
        out.append(f'<path d="M{f(x0)},{y0} Q{f(400 + dx)},{f(top - h * 0.5)} {f(x3)},{f(y3)}" stroke="#5C7F3E" stroke-width="7" fill="none" stroke-linecap="round"/>')
        for j in range(1, 7):
            t = j / 7
            x = (1 - t) ** 2 * x0 + 2 * (1 - t) * t * (400 + dx) + t * t * x3
            y = (1 - t) ** 2 * y0 + 2 * (1 - t) * t * (top - h * 0.5) + t * t * y3
            for side in (-1, 1):
                a = side * 52 + dx * 0.3
                c = rnd.choice(["#2F6B36", "#3A7A40", "#285C30"])
                out.append(
                    f'<g transform="translate({f(x)} {f(y)}) rotate({f(a)})"><ellipse cx="0" cy="-26" rx="13" ry="28" fill="{c}"/>'
                    f'<ellipse cx="-4" cy="-32" rx="4" ry="12" fill="#fff" opacity=".22"/></g>'
                )
        out.append(f'<ellipse cx="{f(x3)}" cy="{f(y3 - 20)}" rx="12" ry="26" fill="#2F6B36"/>')
    return "".join(out), ""


def rosette(cx, cy, r, base, tip, layers=4, tilt=0.78):
    out = [f'<g transform="translate({f(cx)} {f(cy)}) scale(1 {tilt})">']
    for li in range(layers):
        n = [11, 9, 7, 5][min(li, 3)]
        L = r * (1 - li * 0.2)
        w = L * 0.36
        off = li * 17
        shade = li / max(1, layers - 1)
        fill = _mix(base, "#E9F1E4", shade * 0.45)
        for k in range(n):
            a = k * 360 / n + off
            out.append(
                f'<g transform="rotate({f(a)})"><path d="M0,0 C{f(-w)},{f(-L * 0.3)} {f(-w * 0.6)},{f(-L * 0.86)} 0,{f(-L)} '
                f'C{f(w * 0.6)},{f(-L * 0.86)} {f(w)},{f(-L * 0.3)} 0,0Z" fill="{fill}" stroke="{_mix(base, "#000000", 0.2)}" stroke-width="1.5"/>'
                f'<path d="M0,{f(-L)} C{f(w * 0.3)},{f(-L * 0.9)} {f(w * 0.25)},{f(-L * 0.8)} 0,{f(-L * 0.78)} '
                f'C{f(-w * 0.25)},{f(-L * 0.8)} {f(-w * 0.3)},{f(-L * 0.9)} 0,{f(-L)}Z" fill="{tip}" opacity=".85"/></g>'
            )
    out.append(f'<circle r="{f(r * 0.12)}" fill="{_mix(base, "#fff", 0.5)}"/></g>')
    return "".join(out)


def _hex6(c):
    c = c.lstrip("#")
    return "".join(ch * 2 for ch in c) if len(c) == 3 else c


def _mix(a, b, t):
    a = _hex6(a)
    b = _hex6(b)
    ca = [int(a[i:i + 2], 16) for i in (0, 2, 4)]
    cb = [int(b[i:i + 2], 16) for i in (0, 2, 4)]
    return "#" + "".join(f"{round(x + (y - x) * t):02X}" for x, y in zip(ca, cb))


def plant_echeveria(top=540):
    return soil(top=top, w=260) + rosette(400, top - 62, 120, "#7FA89C", "#D98C8C"), ""


def plant_succulent_trio(top=540):
    s = soil(top=top, w=300)
    s += rosette(300, top - 46, 78, "#8FB39A", "#C98A9B")
    s += rosette(500, top - 42, 74, "#A5B77A", "#D9A05B")
    s += rosette(400, top - 66, 96, "#7FA89C", "#D98C8C")
    return s, ""


def spines(x1, y1, x2, y2, count, color="#F2E6B8"):
    out = []
    for i in range(count):
        t = (i + 0.5) / count
        x = x1 + (x2 - x1) * t
        y = y1 + (y2 - y1) * t
        out.append(f'<path d="M{f(x - 4)},{f(y - 3)} l8,6 M{f(x + 4)},{f(y - 3)} l-8,6" stroke="{color}" stroke-width="1.8"/>')
    return "".join(out)


def flower(x, y, r, petal="#E88AA3", center="#F5D06A", n=6):
    out = [f'<g transform="translate({f(x)} {f(y)})">']
    for k in range(n):
        out.append(f'<ellipse cx="0" cy="{f(-r * 0.6)}" rx="{f(r * 0.38)}" ry="{f(r * 0.6)}" fill="{petal}" transform="rotate({f(k * 360 / n)})"/>')
    out.append(f'<circle r="{f(r * 0.3)}" fill="{center}"/></g>')
    return "".join(out)


def plant_cereus(top=520):
    c1, c2 = "#4F8A58", "#3E7248"
    out = [soil(top=top)]
    out.append(f'<path d="M400,{top + 10} V250" stroke="{c1}" stroke-width="86" stroke-linecap="round"/>')
    out.append(f'<path d="M400,430 H318 V318" stroke="{c1}" stroke-width="56" stroke-linecap="round" stroke-linejoin="round" fill="none"/>')
    out.append(f'<path d="M400,380 H482 V290" stroke="{c2}" stroke-width="52" stroke-linecap="round" stroke-linejoin="round" fill="none"/>')
    out.append(f'<path d="M428,{top + 10} V250" stroke="#000" stroke-width="30" opacity=".1" stroke-linecap="round"/>')
    for x in (382, 400, 418):
        out.append(f'<path d="M{x},{top} V262" stroke="#2F5E3A" stroke-width="3" opacity=".55"/>')
    out.append(f'<path d="M318,420 V320 M482,372 V292" stroke="#2F5E3A" stroke-width="3" opacity=".55"/>')
    out.append(spines(362, top - 10, 362, 270, 9) + spines(438, top - 10, 438, 270, 9) + spines(294, 410, 294, 324, 4) + spines(506, 366, 506, 296, 4))
    out.append(flower(482, 262, 34) + flower(318, 292, 26, "#F2A7B8"))
    return "".join(out), ""


def plant_barrel(top=540):
    out = [soil(top=top, w=260)]
    cx, cy, rx, ry = 400, top - 70, 128, 108
    out.append(f'<ellipse cx="{cx}" cy="{cy}" rx="{rx}" ry="{ry}" fill="#4E8A55"/>')
    out.append(f'<ellipse cx="{cx + 40}" cy="{cy + 6}" rx="{rx - 40}" ry="{ry - 6}" fill="#000" opacity=".08"/>')
    for k in (0.2, 0.45, 0.7, 0.92):
        out.append(f'<ellipse cx="{cx}" cy="{cy}" rx="{f(rx * k)}" ry="{ry}" fill="none" stroke="#39703F" stroke-width="3"/>')
    rnd = random.Random(4)
    for k in (0.2, 0.45, 0.7, 0.92):
        for side in (-1, 1):
            for j in range(8):
                t = (j + 0.5) / 8
                ang = math.pi * t
                x = cx + side * rx * k * math.sin(ang)
                y = cy - ry * math.cos(ang)
                out.append(
                    f'<path d="M{f(x)},{f(y)} l{f(rnd.uniform(-9, 9))},{f(-8 - rnd.uniform(0, 6))} M{f(x)},{f(y)} l{f(rnd.uniform(-9, 9))},{f(6)}" '
                    f'stroke="#EFCF6A" stroke-width="2"/>'
                )
    out.append(flower(cx, cy - ry + 6, 26, "#F2C94C", "#E08A3C", 8))
    return "".join(out), ""


def plant_aloe(top=520):
    rnd = random.Random(8)
    out = [soil(top=top)]
    leaf = "M0,0 C-20,-30 -16,-128 0,-236 C16,-128 20,-30 0,0Z"
    for a, s, c in [(-70, 0.72, "#8DB39B"), (68, 0.74, "#7FA78F"), (-48, 0.9, "#79A38B"), (46, 0.92, "#8DB39B"),
                    (-24, 1.02, "#8DB39B"), (22, 1.0, "#79A38B"), (-4, 1.1, "#86AE96")]:
        cid = uid("al")
        spots = "".join(
            f'<ellipse cx="{f(rnd.uniform(-9, 9))}" cy="{f(-rnd.uniform(20, 180))}" rx="3" ry="1.8" fill="#EAF2E6"/>' for _ in range(14)
        )
        out.append(
            f'<g transform="translate(400 {top + 8}) rotate({a}) scale({s})"><clipPath id="{cid}"><path d="{leaf}"/></clipPath>'
            f'<path d="{leaf}" fill="{c}"/><g clip-path="url(#{cid})">{spots}<rect x="0" y="-240" width="30" height="240" fill="#000" opacity=".1"/></g></g>'
        )
    return "".join(out), ""


def plant_orchid(top=520, petal="#FBF6F8", lip="#D9568A"):
    out = [soil(top=top)]
    out.append(f'<path d="M392,{top + 6} C380,{top - 150} 420,{top - 300} 520,{top - 320} C570,{top - 328} 600,{top - 300} 610,{top - 260}" '
               f'stroke="#6C7F4A" stroke-width="5" fill="none" stroke-linecap="round"/>')
    out.append(f'<path d="M398,{top + 8} L402,{top - 120}" stroke="#6C7F4A" stroke-width="3" fill="none"/>')
    pts = [(446, top - 262, 0.8), (500, top - 314, 0.95), (560, top - 320, 1.0), (602, top - 286, 0.85), (612, top - 246, 0.6)]
    for x, y, s in pts:
        out.append(
            f'<g transform="translate({x} {y}) scale({s})">'
            f'<ellipse cx="0" cy="-26" rx="12" ry="26" fill="{petal}"/><ellipse cx="-22" cy="16" rx="11" ry="24" fill="{petal}" transform="rotate(-50 -22 16)"/>'
            f'<ellipse cx="22" cy="16" rx="11" ry="24" fill="{petal}" transform="rotate(50 22 16)"/>'
            f'<circle cx="-24" cy="-4" r="24" fill="{petal}"/><circle cx="24" cy="-4" r="24" fill="{petal}"/>'
            f'<circle cx="-24" cy="-4" r="24" fill="{lip}" opacity=".08"/>'
            f'<path d="M-10,4 C-12,26 12,26 10,4 Z" fill="{lip}"/><circle cx="0" cy="0" r="6" fill="#F2D06B"/></g>'
        )
    for a, s, c in [(-62, 1.0, G["dark"]), (58, 1.0, G["mid"]), (-30, 0.8, G["mid"]), (28, 0.78, G["dark"])]:
        out.append(
            f'<g transform="translate(400 {top + 10}) rotate({a}) scale({s} 0.62)"><path d="M0,0 C-48,-30 -52,-150 0,-190 C52,-150 48,-30 0,0Z" fill="{c}"/>'
            f'<path d="M0,-6 V-180" stroke="{G["vein"]}" stroke-width="4" opacity=".6"/></g>'
        )
    return "".join(out), ""


def plant_lavender(top=520):
    rnd = random.Random(12)
    out = [soil(top=top)]
    for i in range(22):
        a = rnd.uniform(-40, 40)
        L = rnd.uniform(200, 320)
        x0 = 400 + rnd.uniform(-60, 60)
        x1 = x0 + math.sin(math.radians(a)) * L
        y1 = top - math.cos(math.radians(a)) * L
        out.append(f'<path d="M{f(x0)},{top + 6} Q{f(x0 + (x1 - x0) * 0.3)},{f(top - L * 0.5)} {f(x1)},{f(y1)}" stroke="#7B9A63" stroke-width="3" fill="none"/>')
        for j in range(7):
            t = 1 - j * 0.06
            bx = x0 + (x1 - x0) * t + rnd.uniform(-2, 2)
            by = top + (y1 - top) * t
            for side in (-1, 1):
                out.append(
                    f'<ellipse cx="{f(bx + side * 4)}" cy="{f(by)}" rx="4.5" ry="7" fill="{rnd.choice(["#8E7CC3", "#A08FD3", "#7B69B0"])}" '
                    f'transform="rotate({f(a + side * 25)} {f(bx)} {f(by)})"/>'
                )
    for a in (-70, -50, 50, 70, -30, 30):
        out.append(f'<path d="M400,{top + 8} l{f(math.sin(math.radians(a)) * 90)},{f(-math.cos(math.radians(a)) * 90)}" stroke="#93A882" stroke-width="5" stroke-linecap="round"/>')
    return "".join(out), ""


# ---------------------------------------------------------------------------
# Objetos
# ---------------------------------------------------------------------------

def obj_terrarium(mini=False):
    cx, cy, r = 400, 430, 190
    cid = uid("tr")
    out = [
        f'<ellipse cx="{cx}" cy="630" rx="165" ry="24" fill="#8C6440"/><rect x="235" y="606" width="330" height="26" rx="8" fill="#A77B52"/>'
        f'<ellipse cx="{cx}" cy="606" rx="165" ry="22" fill="#B98C60"/>',
        f'<clipPath id="{cid}"><circle cx="{cx}" cy="{cy}" r="{r - 4}"/></clipPath>',
        f'<circle cx="{cx}" cy="{cy}" r="{r}" fill="#FFFFFF" opacity=".35"/>',
        f'<g clip-path="url(#{cid})">',
        f'<rect x="200" y="540" width="400" height="100" fill="#D9CBB0"/>',
    ]
    rnd = random.Random(9 if mini else 10)
    for _ in range(40):
        out.append(f'<ellipse cx="{f(rnd.uniform(215, 585))}" cy="{f(rnd.uniform(550, 615))}" rx="{f(rnd.uniform(6, 13))}" ry="{f(rnd.uniform(4, 8))}" fill="{rnd.choice(["#BFB199", "#E8DEC9", "#A89A82", "#F2ECE0"])}"/>')
    out.append('<path d="M200,548 Q300,520 400,532 T600,540 V560 H200Z" fill="#6A4E3A"/>')
    if mini:
        out.append(rosette(340, 520, 46, "#8FB39A", "#C98A9B", 3))
        out.append(rosette(455, 522, 56, "#7FA89C", "#D98C8C", 3))
        out.append(f'<path d="M520,540 V470" stroke="#4F8A58" stroke-width="30" stroke-linecap="round"/>' + spines(508, 530, 508, 480, 4) + spines(532, 530, 532, 480, 4))
        out.append('<ellipse cx="400" cy="545" rx="30" ry="12" fill="#9A9187"/><ellipse cx="275" cy="540" rx="18" ry="9" fill="#B4ABA0"/>')
    else:
        out.append('<path d="M200,540 C240,500 290,505 320,530 C350,500 400,505 430,530 C470,498 540,502 600,536 V560 H200Z" fill="#6E9A48"/>')
        for _ in range(60):
            out.append(f'<circle cx="{f(rnd.uniform(215, 590))}" cy="{f(rnd.uniform(515, 545))}" r="{f(rnd.uniform(2, 4))}" fill="{rnd.choice(["#5B8A3A", "#86B45A"])}"/>')
        out.append(fern(460, 522, [(-40, 120), (-10, 140), (20, 130), (50, 110), (-65, 95)], 3, 0.9))
        out.append(rosette(320, 520, 44, "#7FA89C", "#D98C8C", 3))
        out.append('<ellipse cx="380" cy="538" rx="24" ry="12" fill="#9A9187"/>')
        out.append('<path d="M540,540 V470 M548,540 V485 M532,540 V492" stroke="#8C6B4F" stroke-width="4"/><path d="M524,478 q16,-30 34,0 z" fill="#E7A95E"/>')
    out.append("</g>")
    out.append(f'<circle cx="{cx}" cy="{cy}" r="{r}" fill="none" stroke="#B9D0CC" stroke-width="6"/>')
    out.append(f'<ellipse cx="{cx}" cy="{cy - r + 14}" rx="74" ry="15" fill="#F4F9F8" stroke="#B9D0CC" stroke-width="5"/>')
    out.append(f'<path d="M{cx - 150},{cy - 40} A160,160 0 0 1 {cx - 60},{cy - 150}" stroke="#fff" stroke-width="10" fill="none" stroke-linecap="round" opacity=".8"/>')
    out.append(f'<path d="M{cx + 150},{cy + 60} A160,160 0 0 1 {cx + 120},{cy + 110}" stroke="#fff" stroke-width="7" fill="none" stroke-linecap="round" opacity=".6"/>')
    return "".join(out), ""


def moss_ball(cx, cy, r, seed):
    rnd = random.Random(seed)
    out = [f'<circle cx="{cx}" cy="{cy}" r="{r}" fill="#5E7F3E"/>']
    for _ in range(140):
        a = rnd.uniform(0, math.tau)
        d = r * math.sqrt(rnd.random()) * 0.96
        out.append(f'<circle cx="{f(cx + math.cos(a) * d)}" cy="{f(cy + math.sin(a) * d)}" r="{f(rnd.uniform(2, 5))}" fill="{rnd.choice(["#4B6A30", "#739450", "#678A45"])}"/>')
    out.append(f'<path d="M{cx + r * 0.1},{cy - r} A{r},{r} 0 0 1 {cx + r * 0.1},{cy + r} A{r * 0.8},{r} 0 0 0 {cx + r * 0.1},{cy - r}Z" fill="#000" opacity=".12"/>')
    for k, off in enumerate((-0.45, 0.05, 0.5)):
        out.append(
            f'<path d="M{f(cx - r * 0.97)},{f(cy + off * r - 10)} Q{f(cx)},{f(cy + off * r + 30)} {f(cx + r * 0.97)},{f(cy + off * r - 12)}" '
            f'stroke="#D2B48A" stroke-width="3.5" fill="none"/>'
        )
    out.append(f'<path d="M{f(cx - 40)},{f(cy - r + 8)} Q{f(cx - 60)},{f(cy)} {f(cx - 30)},{f(cy + r - 5)}" stroke="#D2B48A" stroke-width="3.5" fill="none"/>')
    return "".join(out)


def obj_kokedama_fern():
    out = [
        '<ellipse cx="400" cy="640" rx="170" ry="26" fill="#E7DDCD"/><ellipse cx="400" cy="632" rx="150" ry="20" fill="#F4EEE3"/>',
        fern(400, 410, [(-70, 190), (70, 190), (-45, 230), (45, 230), (-18, 250), (18, 245), (-88, 150), (88, 150)], 13),
        moss_ball(400, 505, 125, 2),
    ]
    return "".join(out), ""


def obj_kokedama_hanging():
    out = [
        '<path d="M400,-10 L310,420 M400,-10 L490,420 M400,-10 L400,380" stroke="#C9AA7D" stroke-width="3"/>',
        '<circle cx="400" cy="30" r="12" fill="none" stroke="#B08C5C" stroke-width="5"/>',
    ]
    back = []
    for x, y, a, c in [(360, 340, -30, G["mid"]), (440, 335, 30, G["light"]), (400, 322, 0, G["dark"]), (330, 365, -60, G["light"]), (470, 360, 60, G["mid"])]:
        back.append(heart_leaf(x, y, a, 1.2, c))
    ball = moss_ball(400, 440, 110, 6)
    vines = [
        ((330, 460), (270, 520), (280, 620), (262, 720)),
        ((470, 470), (540, 520), (540, 640), (548, 740)),
        ((400, 540), (380, 600), (420, 660), (398, 760)),
    ]
    front = trailing_vines(vines, 540, 17)
    return "".join(out) + "".join(back) + ball, front


def obj_macrame(top=470):
    out = []
    for x in (300, 340, 460, 500):
        out.append(f'<path d="M400,40 Q{f((x + 400) / 2)},{f(top * 0.55)} {x},{top + 40}" stroke="#E3D3B8" stroke-width="5" fill="none"/>')
    out.append('<circle cx="400" cy="40" r="16" fill="none" stroke="#B08C5C" stroke-width="7"/><rect x="386" y="56" width="28" height="30" rx="8" fill="#E3D3B8"/>')
    for y in (300, 340):
        out.append(f'<rect x="{f(400 - (y - 40) * 0.36)}" y="{y}" width="14" height="22" rx="6" fill="#D9C6A5"/><rect x="{f(386 + (y - 40) * 0.36)}" y="{y}" width="14" height="22" rx="6" fill="#D9C6A5"/>')
    back, front = plant_pothos(top)
    p = pot("tapered", "cream", top=top, w=190, h=150)
    net = "".join(
        f'<path d="M{x},{top + 30} L{x + 60},{top + 150} M{x + 120},{top + 30} L{x + 60},{top + 150}" stroke="#E3D3B8" stroke-width="5"/>' for x in (290, 390)
    )
    tassel = "".join(f'<path d="M{394 + i * 3},{top + 150} V{top + 250 + (i % 3) * 8}" stroke="#E3D3B8" stroke-width="3"/>' for i in range(5))
    knot = f'<rect x="384" y="{top + 140}" width="32" height="26" rx="10" fill="#D9C6A5"/>'
    return "".join(out) + back + p + net + tassel + knot, front


def obj_pot_only(color="terracotta", kind="tapered", pattern=None):
    return pot(kind, color, top=430, w=260, h=250, pattern=pattern, inner=(kind == "tapered")), ""


def obj_pot_saucer():
    out = ['<ellipse cx="400" cy="672" rx="175" ry="24" fill="#C9B596"/><ellipse cx="400" cy="664" rx="160" ry="18" fill="#DCCAAB"/>']
    out.append(pot("bowl", "sand", top=450, w=280, h=210, pattern="dots"))
    out.append('<ellipse cx="400" cy="451" rx="146" ry="14" fill="#6E5A45"/>')
    out.append(rosette(400, 440, 70, "#7FA89C", "#D98C8C", 3))
    # Borde redibujado por encima del sustrato para que la planta quede "dentro" del macetero.
    rim = POTS["sand"]
    out.append(f'<rect x="251" y="450" width="298" height="22" rx="11" fill="{rim[0]}"/><rect x="266" y="456" width="80" height="5" rx="2.5" fill="{rim[2]}"/>')
    return "".join(out), ""


def obj_cement_set():
    out = []
    specs = [(265, 470, 150, 190, "rosette1"), (520, 430, 180, 230, "cactus"), (395, 530, 120, 130, "rosette2")]
    for cx, top, w, h, plant in specs:
        out.append(soil(cx, top, w - 10))
        if plant == "cactus":
            out.append(f'<path d="M{cx},{top + 5} V{top - 110}" stroke="#4F8A58" stroke-width="54" stroke-linecap="round"/>')
            out.append(spines(cx - 22, top - 5, cx - 22, top - 110, 5) + spines(cx + 22, top - 5, cx + 22, top - 110, 5))
        elif plant == "rosette1":
            out.append(rosette(cx, top - 22, 64, "#8FB39A", "#C98A9B", 3))
        else:
            out.append(rosette(cx, top - 16, 48, "#A5B77A", "#D9A05B", 3))
        out.append(pot("cylinder", "cement", cx=cx, top=top, w=w, h=h, pattern="dots" if plant == "cactus" else None))
    return "".join(out), ""


def obj_substrate_bag():
    out = [
        '<path d="M270,250 L530,250 L560,660 Q560,680 540,680 H260 Q240,680 240,660 Z" fill="#C9A77C"/>',
        '<path d="M430,250 L530,250 L560,660 Q560,680 540,680 H470 Z" fill="#B08D62"/>',
        '<path d="M262,250 L284,210 L516,210 L538,250 Z" fill="#B99567"/>',
        '<path d="M280,232 H520" stroke="#9F7C52" stroke-width="3" stroke-dasharray="10 8"/>',
        '<rect x="300" y="360" width="200" height="190" rx="18" fill="#FBF6EC"/>',
        f'<g transform="translate(400 430) rotate(-20)"><path d="M0,30 C-30,0 -20,-40 0,-50 C20,-40 30,0 0,30Z" fill="{G["mid"]}"/><path d="M0,28 V-44" stroke="{G["vein"]}" stroke-width="3"/></g>',
        '<rect x="330" y="490" width="140" height="10" rx="5" fill="#1F3D2B" opacity=".8"/><rect x="350" y="512" width="100" height="8" rx="4" fill="#1F3D2B" opacity=".35"/>',
        f'<path d="M190,690 Q230,630 290,640 Q330,600 380,650 Q420,690 190,690Z" fill="{SOIL}"/>',
    ]
    rnd = random.Random(2)
    for _ in range(30):
        out.append(f'<circle cx="{f(rnd.uniform(215, 360))}" cy="{f(rnd.uniform(650, 686))}" r="{f(rnd.uniform(2, 4))}" fill="{rnd.choice(["#6B4E3A", "#E8DCC4", "#8A6A50"])}"/>')
    return "".join(out), ""


def obj_fertilizer():
    out = [
        '<rect x="320" y="300" width="160" height="380" rx="40" fill="#8A4B25"/>',
        '<rect x="420" y="300" width="60" height="380" rx="30" fill="#000" opacity=".15"/>',
        '<rect x="350" y="240" width="100" height="70" rx="10" fill="#E8DFCF"/><rect x="345" y="220" width="110" height="36" rx="10" fill="#1F3D2B"/>',
        '<rect x="338" y="400" width="124" height="170" rx="14" fill="#FBF6EC"/>',
        f'<g transform="translate(400 470)"><path d="M0,30 C-30,0 -20,-40 0,-50 C20,-40 30,0 0,30Z" fill="{G["mid"]}"/><path d="M0,28 V-44" stroke="{G["vein"]}" stroke-width="3"/></g>',
        '<rect x="360" y="530" width="80" height="8" rx="4" fill="#1F3D2B" opacity=".7"/>',
        '<rect x="334" y="316" width="14" height="330" rx="7" fill="#fff" opacity=".2"/>',
        '<path d="M560,560 C540,590 540,620 560,630 C580,620 580,590 560,560Z" fill="#6FA4B5" opacity=".9"/>',
    ]
    return "".join(out), ""


def obj_watering_can():
    brass, dark, light = "#C9A15A", "#A8823F", "#E6C98C"
    out = [
        f'<path d="M470,470 L640,300" stroke="{dark}" stroke-width="20" stroke-linecap="round"/>',
        f'<g transform="translate(646 294) rotate(-45)"><path d="M-30,-18 H30 L22,12 H-22Z" fill="{brass}"/><rect x="-32" y="-26" width="64" height="12" rx="6" fill="{dark}"/></g>',
        f'<path d="M290,380 C260,300 330,240 400,250 C470,260 520,320 490,390" stroke="{dark}" stroke-width="18" fill="none" stroke-linecap="round"/>',
        f'<path d="M270,400 Q270,370 300,370 H500 Q530,370 530,400 V650 Q530,680 500,680 H300 Q270,680 270,650Z" fill="{brass}"/>',
        f'<path d="M420,370 H500 Q530,370 530,400 V650 Q530,680 500,680 H420Z" fill="{dark}" opacity=".6"/>',
        f'<rect x="290" y="390" width="18" height="260" rx="9" fill="{light}" opacity=".75"/>',
        f'<rect x="262" y="360" width="276" height="24" rx="12" fill="{dark}"/>',
        f'<rect x="270" y="520" width="260" height="10" fill="{dark}" opacity=".5"/>',
    ]
    return "".join(out), ""


def obj_mister():
    out = [
        '<path d="M330,380 Q330,340 370,340 H430 Q470,340 470,380 V650 Q470,680 440,680 H360 Q330,680 330,650Z" fill="#D8EEF0" opacity=".85"/>',
        '<path d="M334,470 H466 V650 Q466,676 440,676 H360 Q334,676 334,650Z" fill="#9FCBD3" opacity=".7"/>',
        '<path d="M330,380 Q330,340 370,340 H430 Q470,340 470,380 V650 Q470,680 440,680 H360 Q330,680 330,650Z" fill="none" stroke="#9DB9BD" stroke-width="5"/>',
        '<rect x="346" y="370" width="12" height="270" rx="6" fill="#fff" opacity=".7"/>',
        '<rect x="370" y="300" width="60" height="44" rx="6" fill="#2E332F"/><path d="M360,240 H450 Q470,240 470,262 V300 H360Z" fill="#C9A15A"/>',
        '<path d="M360,250 H300 Q286,250 286,262 V270 H360Z" fill="#A8823F"/><path d="M420,300 Q440,330 470,334" stroke="#2E332F" stroke-width="12" fill="none" stroke-linecap="round"/>',
        '<g fill="#9FCBD3"><circle cx="250" cy="250" r="6"/><circle cx="226" cy="236" r="4"/><circle cx="232" cy="270" r="5"/><circle cx="204" cy="256" r="3.5"/><circle cx="210" cy="222" r="3"/></g>',
    ]
    return "".join(out), ""


def obj_tools():
    out = [
        '<g transform="translate(330 420) rotate(-28)"><rect x="-22" y="40" width="44" height="190" rx="22" fill="#A6784E"/>'
        '<rect x="-6" y="60" width="8" height="150" rx="4" fill="#C4966A"/><rect x="-10" y="-6" width="20" height="56" rx="6" fill="#8E9499"/>'
        '<path d="M0,-240 C-60,-180 -64,-60 -40,-10 H40 C64,-60 60,-180 0,-240Z" fill="#B9C0C5"/><path d="M0,-240 C60,-180 64,-60 40,-10 H0Z" fill="#9AA2A8"/></g>',
        '<g transform="translate(480 430) rotate(24)"><rect x="-20" y="50" width="40" height="180" rx="20" fill="#A6784E"/>'
        '<rect x="-8" y="-4" width="16" height="60" rx="5" fill="#8E9499"/><path d="M-50,-10 H50" stroke="#9AA2A8" stroke-width="14" stroke-linecap="round"/>'
        '<path d="M-44,-10 V-170 M0,-10 V-190 M44,-10 V-170" stroke="#B9C0C5" stroke-width="12" stroke-linecap="round"/></g>',
        '<g transform="translate(580 600)"><circle r="46" fill="#D9C19A"/><circle r="46" fill="none" stroke="#B89A6C" stroke-width="3" stroke-dasharray="4 5"/><circle r="14" fill="#8C6B4F"/></g>',
    ]
    return "".join(out), ""


def obj_gift_box():
    out = [
        '<rect x="300" y="420" width="300" height="250" rx="12" fill="#E6B8A2"/>',
        '<rect x="480" y="420" width="120" height="250" rx="12" fill="#D09A80" opacity=".7"/>',
        '<rect x="285" y="385" width="330" height="60" rx="12" fill="#EFC6B3"/>',
        f'<rect x="425" y="385" width="50" height="285" fill="{G["dark"]}"/>',
        f'<path d="M450,385 C400,320 340,340 370,380 Z M450,385 C500,320 560,340 530,380Z" fill="{G["mid"]}"/>',
        f'<circle cx="450" cy="385" r="16" fill="{G["dark"]}"/>',
        '<g transform="translate(560 560) rotate(12)"><rect x="0" y="0" width="96" height="62" rx="8" fill="#FBF6EC"/><circle cx="14" cy="14" r="5" fill="#C9A15A"/>'
        '<path d="M26,30 H80 M26,44 H66" stroke="#1F3D2B" stroke-width="4" stroke-linecap="round" opacity=".6"/></g>',
        '<path d="M612,512 Q590,530 580,570" stroke="#C9A15A" stroke-width="2" fill="none"/>',
    ]
    small = soil(210, 560, 110) + rosette(210, 546, 58, "#7FA89C", "#D98C8C", 3) + pot("tapered", "terracotta", cx=210, top=560, w=110, h=120)
    return small + "".join(out), ""


# ---------------------------------------------------------------------------
# Composición
# ---------------------------------------------------------------------------

def scene(bgkey, parts, vb="0 0 800 800", title="", shadow_y=690, shadow_rx=190):
    bg, sun = BG[bgkey]
    back, front = parts
    s = f'<rect x="-400" y="-400" width="1600" height="1600" fill="{bg}"/>'
    s += f'<circle cx="400" cy="370" r="262" fill="{sun}"/>'
    s += f'<ellipse cx="400" cy="{shadow_y}" rx="{shadow_rx}" ry="20" fill="{SHADOW}" opacity=".1"/>'
    return svg(s + back + front, vb, title)


def with_pot(plant_fn, potkind="tapered", color="terracotta", pattern=None, top=520, w=210, h=165, **kw):
    def build(bgkey):
        back, front = plant_fn(**kw) if kw else plant_fn()
        return back + pot(potkind, color, top=top, w=w, h=h, pattern=pattern), front
    return build


def monstera_builder(color, pattern=None):
    def build(bgkey):
        back, front = plant_monstera(BG[bgkey][1])
        return back + pot("tapered", color, pattern=pattern), front
    return build


def simple(fn, *args):
    def build(bgkey):
        return fn(*args)
    return build


PRODUCTS = {
    # slug: (builder, [bg variants], detail viewBox, title)
    "monstera-deliciosa": (monstera_builder("terracotta"), ["sage", "sand", "cream"], "150 90 420 420"),
    "sansevieria-trifasciata": (with_pot(plant_snake, "cylinder", "cream", "stripes"), ["sand", "sage", "blush"], "230 120 340 340"),
    "potus-dorado": (with_pot(plant_pothos, "tapered", "white"), ["mist", "sand", "sage"], "180 400 380 380"),
    "ficus-lyrata": (with_pot(plant_fiddle, "tapered", "charcoal"), ["cream", "sage", "sand"], "230 150 340 340"),
    "spathiphyllum": (with_pot(plant_peace_lily, "tapered", "white", "band"), ["sage", "mist", "cream"], "250 150 300 300"),
    "helecho-de-boston": (with_pot(plant_fern, "bowl", "sage", None, 520, 230, 160), ["mist", "sage", "sand"], "150 220 360 360"),
    "pilea-peperomioides": (with_pot(plant_pilea, "tapered", "blush"), ["cream", "blush", "sage"], "230 250 340 340"),
    "zamioculca": (with_pot(plant_zz, "cylinder", "charcoal"), ["sand", "sage", "cream"], "260 140 320 320"),
    "orquidea-phalaenopsis": (with_pot(plant_orchid, "cylinder", "white"), ["lilac", "blush", "cream"], "400 150 260 260"),
    "echeveria-elegans": (with_pot(plant_echeveria, "bowl", "terracotta", None, 540, 260, 140), ["blush", "sand", "sage"], "260 350 280 280"),
    "trio-de-suculentas": (with_pot(plant_succulent_trio, "bowl", "cream", "dots", 540, 320, 140), ["sand", "blush", "sage"], "200 380 300 300"),
    "cactus-cereus": (with_pot(plant_cereus, "tapered", "terracotta", "arches"), ["sand", "blush", "cream"], "260 190 300 300"),
    "cactus-bola-dorada": (with_pot(plant_barrel, "bowl", "charcoal", None, 540, 260, 140), ["cream", "sand", "blush"], "280 330 240 240"),
    "aloe-vera": (with_pot(plant_aloe, "tapered", "cement"), ["sage", "cream", "mist"], "260 280 280 280"),
    "terrario-esfera-bosque": (simple(obj_terrarium, False), ["mist", "sage", "cream"], "250 390 300 220"),
    "terrario-mini-suculentas": (simple(obj_terrarium, True), ["blush", "cream", "sand"], "250 400 300 220"),
    "kokedama-helecho": (simple(obj_kokedama_fern), ["sage", "cream", "mist"], "250 330 300 300"),
    "kokedama-colgante-potus": (simple(obj_kokedama_hanging), ["mist", "sand", "sage"], "230 300 340 340"),
    "maceta-terracota-17": (simple(obj_pot_only, "terracotta", "tapered", "band"), ["cream", "sand", "sage"], "250 380 300 300"),
    "macetero-ceramica-arena": (simple(obj_pot_saucer), ["sage", "cream", "blush"], "220 420 360 280"),
    "colgante-macrame": (simple(obj_macrame), ["sand", "cream", "mist"], "250 0 300 300"),
    "set-macetas-cemento": (simple(obj_cement_set), ["cream", "sand", "sage"], "400 280 280 280"),
    "sustrato-premium-5l": (simple(obj_substrate_bag), ["sand", "cream", "sage"], "280 330 240 240"),
    "fertilizante-organico": (simple(obj_fertilizer), ["sage", "cream", "mist"], "300 360 200 200"),
    "regadera-laton": (simple(obj_watering_can), ["cream", "sage", "sand"], "450 230 250 250"),
    "pulverizador-vidrio": (simple(obj_mister), ["mist", "cream", "sage"], "190 220 300 300"),
    "set-herramientas": (simple(obj_tools), ["sand", "sage", "cream"], "180 160 300 300"),
    "caja-regalo-primer-jardin": (simple(obj_gift_box), ["blush", "cream", "sage"], "280 300 360 360"),
    "kit-lavanda": (with_pot(plant_lavender, "tapered", "cream", "band"), ["lilac", "cream", "sage"], "250 150 300 300"),
}

CATEGORIES = {
    "plantas-de-interior": (monstera_builder("cream", "band"), "sage"),
    "suculentas-y-cactus": (with_pot(plant_succulent_trio, "bowl", "terracotta", None, 540, 320, 140), "blush"),
    "terrarios-y-kokedamas": (simple(obj_terrarium, False), "mist"),
    "macetas-y-maceteros": (simple(obj_cement_set), "sand"),
    "cuidado-y-accesorios": (simple(obj_watering_can), "cream"),
    "regalos": (simple(obj_gift_box), "lilac"),
}


def write(path: Path, content: str):
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(content, encoding="utf-8")


def place(parts, dx, dy, s):
    back, front = parts
    return f'<g transform="translate({f(dx)} {f(dy)}) scale({f(s)})">{back}{front}</g>'


def hero():
    """Composición principal: ventana en arco con varias plantas (1000x1100)."""
    out = [
        '<defs><linearGradient id="hwall" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="#EAF0E4"/><stop offset="1" stop-color="#DCE6D4"/></linearGradient>'
        '<linearGradient id="hlight" x1="0" y1="0" x2="1" y2="1"><stop offset="0" stop-color="#FFF7E6" stop-opacity=".95"/><stop offset="1" stop-color="#FFF7E6" stop-opacity="0"/></linearGradient></defs>',
        '<rect width="1000" height="1100" fill="url(#hwall)"/>',
        '<path d="M250,760 V330 A250,250 0 0 1 750,330 V760Z" fill="#F7F1E4"/>',
        '<path d="M270,760 V335 A230,230 0 0 1 730,335 V760Z" fill="#CFE0D9"/>',
        '<path d="M500,105 V760 M270,470 H730" stroke="#F7F1E4" stroke-width="14"/>',
        '<path d="M270,760 L730,335 V760Z" fill="url(#hlight)" opacity=".5"/>',
        '<rect x="0" y="760" width="1000" height="340" fill="#E9DCC6"/>',
        '<rect x="0" y="760" width="1000" height="14" fill="#D8C7AA"/>',
    ]
    mon = plant_monstera("#CFE0D9")
    out.append('<ellipse cx="360" cy="884" rx="200" ry="22" fill="#1F3D2B" opacity=".12"/>')
    out.append('<ellipse cx="800" cy="882" rx="120" ry="16" fill="#1F3D2B" opacity=".12"/>')
    out.append('<ellipse cx="578" cy="890" rx="80" ry="12" fill="#1F3D2B" opacity=".12"/>')
    out.append(place((mon[0] + pot("tapered", "terracotta", pattern="band"), ""), 20, 300, 0.85))
    out.append(place(with_pot(plant_snake, "cylinder", "cream", "stripes")("sand"), 520, 400, 0.7))
    out.append(place(with_pot(plant_echeveria, "bowl", "blush", None, 540, 260, 140)("sand"), 410, 600, 0.42))
    pothos = with_pot(plant_pothos, "tapered", "white")("mist")
    out.append('<path d="M800,0 L745,142 M800,0 L855,142" stroke="#C9AA7D" stroke-width="3"/>')
    out.append(place(pothos, 600, -120, 0.5))
    return svg("".join(out), "0 0 1000 1000", "Plantas de JardinNoemi junto a una ventana en arco", 1000, 1000)


def about_scene():
    out = [
        '<rect width="900" height="700" fill="#F4EDE1"/>',
        '<path d="M130,300 V150 A90,90 0 0 1 310,150 V300Z" fill="#E6EDE1"/><path d="M590,300 V170 A70,70 0 0 1 730,170 V300Z" fill="#F7E6DC"/>',
        '<circle cx="450" cy="150" r="46" fill="#E9DCC6"/>',
        '<rect x="0" y="470" width="900" height="230" fill="#E4D5BB"/>',
        '<rect x="80" y="440" width="740" height="34" rx="8" fill="#B98C60"/><rect x="110" y="474" width="24" height="226" fill="#A77B52"/><rect x="766" y="474" width="24" height="226" fill="#A77B52"/>',
    ]
    items = [
        (with_pot(plant_fern, "bowl", "sage", None, 520, 230, 160)("sage"), 30, 157, 0.42),
        (simple(obj_terrarium, True)("blush"), 215, 210, 0.36),
        (with_pot(plant_pilea, "tapered", "blush")("cream"), 395, 198, 0.36),
        (with_pot(plant_cereus, "tapered", "terracotta", "arches")("sand"), 545, 205, 0.35),
        (simple(obj_watering_can)("cream"), 640, 212, 0.34),
    ]
    for parts, dx, dy, s in items:
        out.append(place(parts, dx, dy, s))
    return svg("".join(out), "0 0 900 700", "Mesa de trabajo del taller de JardinNoemi", 900, 700)


def main():
    for slug, (builder, bgs, detail_vb) in PRODUCTS.items():
        for i, bg in enumerate(bgs, start=1):
            _uid[0] = 0
            parts = builder(bg)
            vb = {1: "0 0 800 800", 2: "100 70 600 600", 3: detail_vb}[i]
            write(OUT / "products" / f"{slug}-{i}.svg", scene(bg, parts, vb))
    for slug, (builder, bg) in CATEGORIES.items():
        _uid[0] = 0
        write(OUT / "categories" / f"{slug}.svg", scene(bg, builder(bg), "40 40 720 720"))
    _uid[0] = 0
    write(OUT / "brand" / "hero.svg", hero())
    _uid[0] = 0
    write(OUT / "brand" / "about-workshop.svg", about_scene())
    print(f"OK: {len(PRODUCTS) * 3} imágenes de producto, {len(CATEGORIES)} de categoría, 2 de marca -> {OUT}")


if __name__ == "__main__":
    main()
