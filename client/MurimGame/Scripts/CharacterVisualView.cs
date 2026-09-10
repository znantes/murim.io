using Godot;
using Murim.Simulation;

namespace Murim.Game;

/// <summary>
/// Lightweight full-body beta renderer driven by the same genes, ageing state and injuries that will
/// later feed the realistic 3D portrait/body pipeline. It renders only characters currently visible.
/// </summary>
public partial class CharacterVisualView : Control
{
    private Npc? npc;
    private PortraitGenome? genome;
    private PortraitAppearanceState? appearance;
    private bool compact;

    public CharacterVisualView()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        CustomMinimumSize = new Vector2(170, 260);
    }

    public void SetCharacter(Npc value, PortraitGenome genes, PortraitAppearanceState state, bool compactMode = false)
    {
        npc = value;
        genome = genes;
        appearance = state;
        compact = compactMode;
        CustomMinimumSize = compact ? new Vector2(72, 104) : new Vector2(170, 260);
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (npc is null || genome is null || appearance is null) return;

        var w = Math.Max(50f, Size.X);
        var h = Math.Max(90f, Size.Y);
        var stage = appearance.GrowthStage;
        var infant = stage == FacialGrowthStage.Infant;
        var child = stage == FacialGrowthStage.Child;
        var adolescent = stage == FacialGrowthStage.Adolescent;

        var skin = SkinColor(genome.SkinTone, appearance.Pallor);
        var hair = HairColor(genome.HairPigment, appearance.GreyHairAmount);
        var cloth = ClothingColor(npc.Id);
        var outline = new Color("344552");
        var wound = new Color("b54c50");

        var cx = w * .5f;
        var floor = h * .93f;
        var headR = infant ? h * .105f : child ? h * .087f : adolescent ? h * .075f : h * .069f;
        var torsoTop = infant ? h * .35f : child ? h * .29f : h * .235f;
        var torsoBottom = infant ? h * .62f : child ? h * .65f : h * .62f;
        var shoulders = (float)(w * (.15 + genome.BodyFrameWidth * .075));
        var hips = (float)(w * (.105 + genome.BodyAdipositySetPoint * .075));
        shoulders *= (float)(1 + genome.BodyAdipositySetPoint * .14);
        if (npc.Identity.Sex == Sex.Female) hips *= 1.08f;

        var head = new Vector2(cx, torsoTop - headR * 1.34f);

        DrawSetTransform(new Vector2(cx, floor), 0, new Vector2(1.4f, .28f));
        DrawCircle(Vector2.Zero, Math.Max(12, w * .13f), new Color(0, 0, 0, .10f));
        DrawSetTransform(Vector2.Zero, 0, Vector2.One);

        DrawLeg(BodySide.Left, cx - hips * .42f, torsoBottom, h * .76f, h * .89f, floor, cloth, skin, outline);
        DrawLeg(BodySide.Right, cx + hips * .42f, torsoBottom, h * .76f, h * .89f, floor, cloth, skin, outline);

        var torso = new PackedVector2Array(new[]
        {
            new Vector2(cx - shoulders, torsoTop), new Vector2(cx + shoulders, torsoTop),
            new Vector2(cx + hips, torsoBottom), new Vector2(cx - hips, torsoBottom)
        });
        DrawColoredPolygon(torso, cloth);
        DrawPolyline(new PackedVector2Array(new[] { torso[0], torso[1], torso[2], torso[3], torso[0] }), outline, compact ? 1.1f : 2f, true);
        var sashY = Mathf.Lerp(torsoTop, torsoBottom, .63f);
        DrawLine(new Vector2(cx - hips, sashY), new Vector2(cx + hips, sashY), cloth.Lightened(.18f), compact ? 2 : 4, true);

        var shoulderY = torsoTop + (torsoBottom - torsoTop) * .13f;
        DrawArm(BodySide.Left, new Vector2(cx - shoulders, shoulderY), new Vector2(cx - shoulders * 1.42f, h * .48f), new Vector2(cx - shoulders * 1.30f, h * .64f), cloth, skin, outline);
        DrawArm(BodySide.Right, new Vector2(cx + shoulders, shoulderY), new Vector2(cx + shoulders * 1.42f, h * .48f), new Vector2(cx + shoulders * 1.30f, h * .64f), cloth, skin, outline);

        DrawLine(new Vector2(cx, head.Y + headR * .72f), new Vector2(cx, torsoTop + 2), skin, Math.Max(4, headR * .36f), true);

        var faceW = (float)(.82 + genome.FaceWidth * .26 + (infant ? genome.ChildhoodRoundness * .10 : 0));
        var faceH = (float)(1.03 + genome.JawLength * .10 + (adolescent ? genome.AdolescentLengthening * .07 : 0));
        DrawSetTransform(head, 0, new Vector2(faceW, faceH));
        DrawCircle(Vector2.Zero, headR, skin);
        DrawArc(Vector2.Zero, headR, 0, Mathf.Tau, 30, outline, compact ? 1 : 1.5f, true);
        DrawSetTransform(Vector2.Zero, 0, Vector2.One);

        DrawHair(head, headR, hair);
        if (!compact) DrawFace(head, headR, skin, hair);
        DrawInjuryMarkers(wound, cx, head, headR, torsoTop, torsoBottom, shoulders, hips, h);
    }

    private void DrawArm(BodySide side, Vector2 shoulder, Vector2 elbow, Vector2 hand, Color cloth, Color skin, Color outline)
    {
        if (IsAmputated(side, BodyRegion.UpperArm)) { DrawAmputationEnd(shoulder, cloth); return; }
        DrawLine(shoulder, elbow, outline, compact ? 6 : 13, true);
        DrawLine(shoulder, elbow, cloth, compact ? 4 : 10, true);
        if (IsAmputated(side, BodyRegion.Forearm)) { DrawAmputationEnd(elbow, skin); return; }
        DrawLine(elbow, hand, skin, compact ? 4 : 9, true);
        if (IsAmputated(side, BodyRegion.Hand) || IsAmputated(side, BodyRegion.Fingers)) DrawAmputationEnd(hand, skin);
        else DrawCircle(hand, compact ? 3.2f : 6.2f, skin);
    }

    private void DrawLeg(BodySide side, float x, float top, float kneeY, float ankleY, float floor, Color cloth, Color skin, Color outline)
    {
        var sign = side == BodySide.Left ? -1f : 1f;
        if (IsAmputated(side, BodyRegion.Thigh)) { DrawAmputationEnd(new Vector2(x, top + 10), cloth); return; }
        var knee = new Vector2(x + sign * 2, kneeY);
        DrawLine(new Vector2(x, top), knee, outline, compact ? 7 : 15, true);
        DrawLine(new Vector2(x, top), knee, cloth, compact ? 5 : 12, true);
        if (IsAmputated(side, BodyRegion.LowerLeg)) { DrawAmputationEnd(knee, skin); return; }
        var ankle = new Vector2(x - sign * 1.5f, ankleY);
        DrawLine(knee, ankle, outline, compact ? 5 : 11, true);
        DrawLine(knee, ankle, skin, compact ? 3.5f : 8, true);
        if (IsAmputated(side, BodyRegion.Foot) || IsAmputated(side, BodyRegion.Toes)) { DrawAmputationEnd(ankle, skin); return; }
        var foot = new Vector2(x + sign * (compact ? 6 : 11), floor);
        DrawLine(ankle, foot, outline, compact ? 4 : 8, true);
        DrawLine(ankle, foot, cloth.Darkened(.18f), compact ? 2.7f : 6, true);
    }

    private void DrawHair(Vector2 head, float r, Color hair)
    {
        if (genome is null) return;
        DrawSetTransform(new Vector2(head.X, head.Y - r * .20f), 0, new Vector2(1.05f, .62f));
        DrawCircle(Vector2.Zero, r * (float)(.88 + genome.HairDensity * .15), hair);
        DrawSetTransform(Vector2.Zero, 0, Vector2.One);
        if (genome.HairDensity > .38 && !compact)
        {
            var length = r * (float)(.35 + genome.HairDensity * .55);
            DrawLine(new Vector2(head.X - r * .78f, head.Y - r * .08f), new Vector2(head.X - r * .72f, head.Y + length), hair, Math.Max(3, r * .18f), true);
            DrawLine(new Vector2(head.X + r * .78f, head.Y - r * .08f), new Vector2(head.X + r * .72f, head.Y + length), hair, Math.Max(3, r * .18f), true);
        }
    }

    private void DrawFace(Vector2 head, float r, Color skin, Color hair)
    {
        if (genome is null || appearance is null) return;
        var asym = (float)genome.FacialAsymmetry * r * .22f;
        var eyeY = head.Y - r * .06f;
        var eyeGap = r * (float)(.28 + genome.EyeSpacing * .16);
        var eyeR = r * (float)(.055 + genome.EyeSize * .035);
        var eyes = Color.Lerp(new Color("403730"), new Color("718087"), (float)genome.EyePigment * .34f);
        DrawCircle(new Vector2(head.X - eyeGap - asym, eyeY), eyeR, eyes);
        DrawCircle(new Vector2(head.X + eyeGap + asym * .35f, eyeY + asym * .10f), eyeR, eyes);
        var browY = eyeY - r * .16f;
        DrawLine(new Vector2(head.X - eyeGap - r * .12f, browY), new Vector2(head.X - eyeGap + r * .12f, browY - r * .025f), hair, 2, true);
        DrawLine(new Vector2(head.X + eyeGap - r * .12f, browY - r * .02f), new Vector2(head.X + eyeGap + r * .12f, browY), hair, 2, true);
        var nose = new Vector2(head.X + asym * .18f, head.Y + r * (float)(.12 + genome.NoseLength * .16));
        DrawLine(new Vector2(head.X, head.Y), nose, skin.Darkened(.20f), 1.4f, true);
        var mouthY = head.Y + r * .42f;
        var mouthW = r * (float)(.18 + genome.LipFullness * .20);
        DrawLine(new Vector2(head.X - mouthW, mouthY), new Vector2(head.X + mouthW, mouthY + asym * .05f), new Color("9a6460"), 2, true);
        if (appearance.EyeBagAmount > .38)
        {
            var bag = new Color(.25f, .23f, .28f, (float)Math.Clamp(appearance.EyeBagAmount * .28, 0, .28));
            DrawArc(new Vector2(head.X - eyeGap, eyeY + r * .09f), r * .14f, .15f, 2.95f, 12, bag, 2, true);
            DrawArc(new Vector2(head.X + eyeGap, eyeY + r * .09f), r * .14f, .15f, 2.95f, 12, bag, 2, true);
        }
    }

    private void DrawInjuryMarkers(Color color, float cx, Vector2 head, float r, float top, float bottom, float shoulder, float hip, float h)
    {
        if (npc is null) return;
        foreach (var record in npc.Injuries.Where(i => i.Active || i.Permanent).Take(8))
        {
            var side = record.Side == BodySide.Left ? -1f : record.Side == BodySide.Right ? 1f : 0f;
            var p = record.Region switch
            {
                BodyRegion.Head or BodyRegion.Face or BodyRegion.Eye or BodyRegion.Ear => head + new Vector2(side * r * .46f, 0),
                BodyRegion.Neck => new Vector2(cx, top - 5),
                BodyRegion.Chest => new Vector2(cx + side * shoulder * .35f, Mathf.Lerp(top, bottom, .30f)),
                BodyRegion.Abdomen or BodyRegion.Dantian or BodyRegion.InternalOrgans => new Vector2(cx, Mathf.Lerp(top, bottom, .66f)),
                BodyRegion.Shoulder or BodyRegion.UpperArm => new Vector2(cx + side * shoulder * 1.16f, top + 15),
                BodyRegion.Elbow or BodyRegion.Forearm or BodyRegion.Wrist or BodyRegion.Hand or BodyRegion.Fingers => new Vector2(cx + side * shoulder * 1.35f, h * .56f),
                BodyRegion.Hip or BodyRegion.Thigh => new Vector2(cx + side * hip * .75f, h * .66f),
                BodyRegion.Knee => new Vector2(cx + side * hip * .50f, h * .76f),
                BodyRegion.LowerLeg or BodyRegion.Ankle or BodyRegion.Foot or BodyRegion.Toes => new Vector2(cx + side * hip * .45f, h * .88f),
                _ => new Vector2(cx, Mathf.Lerp(top, bottom, .48f))
            };
            var radius = compact ? 2.5f : 4f + (int)record.Severity * .45f;
            DrawCircle(p, radius, color);
            if (record.Permanent) DrawArc(p, radius + 3, 0, Mathf.Tau, 18, new Color("7b1f2b"), 2, true);
        }
    }

    private bool IsAmputated(BodySide side, BodyRegion region)
        => npc?.Injuries.Any(i => i.Permanent && i.Kind == InjuryKind.Amputation && i.Side == side && i.Region == region) == true;

    private void DrawAmputationEnd(Vector2 p, Color color)
    {
        DrawCircle(p, compact ? 3 : 6, color.Darkened(.18f));
        DrawArc(p, compact ? 4 : 8, 0, Mathf.Tau, 16, new Color("8f4a45"), compact ? 1 : 2, true);
    }

    private static Color SkinColor(double tone, double pallor)
    {
        var light = new Color("f1d5bd");
        var medium = new Color("c78d6b");
        var deep = new Color("80523f");
        var baseColor = tone < .52 ? Color.Lerp(light, medium, (float)(tone / .52)) : Color.Lerp(medium, deep, (float)((tone - .52) / .48));
        return Color.Lerp(baseColor, new Color("e8e0dc"), (float)Math.Clamp(pallor * .42, 0, .42));
    }

    private static Color HairColor(double pigment, double grey)
    {
        var dark = Color.Lerp(new Color("181616"), new Color("49352d"), (float)Math.Clamp(pigment * .45, 0, .45));
        return Color.Lerp(dark, new Color("b8b8b3"), (float)Math.Clamp(grey, 0, 1));
    }

    private static Color ClothingColor(Guid id)
    {
        var palette = new[] { new Color("526b72"), new Color("6f6456"), new Color("765d69"), new Color("49665a"), new Color("6a7050"), new Color("555f7a") };
        return palette[id.ToByteArray()[0] % palette.Length];
    }
}
