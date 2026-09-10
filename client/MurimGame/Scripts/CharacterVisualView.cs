using Godot;
using Murim.Simulation;

namespace Murim.Game;

/// <summary>
/// Lightweight full-body paper-doll used by the beta. It is generated from the same portrait genome
/// that will later drive the realistic 3D head/body pipeline, so replacing this renderer will not
/// invalidate genetics, ageing or injuries.
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

    public void SetCharacter(Npc value, PortraitGenome genes, PortraitAppearanceState visibleState, bool compactMode = false)
    {
        npc = value;
        genome = genes;
        appearance = visibleState;
        compact = compactMode;
        CustomMinimumSize = compact ? new Vector2(70, 100) : new Vector2(170, 260);
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (npc is null || genome is null || appearance is null) return;

        var w = Math.Max(40f, Size.X);
        var h = Math.Max(80f, Size.Y);
        var age = npc.AgeYears(new WorldClockProxy(npc.Identity.BirthDay, appearance.ApparentAge));
        // ApparentAge is intentionally used for visual proportions only when it is close to real age.
        var visualAge = Math.Max(0, appearance.ApparentAge);
        var stage = appearance.GrowthStage;
        var infant = stage == FacialGrowthStage.Infant;
        var child = stage == FacialGrowthStage.Child;
        var adolescent = stage == FacialGrowthStage.Adolescent;
        var elder = stage == FacialGrowthStage.Elder;

        var skin = SkinColor(genome.SkinTone, appearance.Pallor);
        var hair = HairColor(genome.HairPigment, appearance.GreyHairAmount);
        var clothing = ClothingColor(npc.Id);
        var outline = new Color("364654");
        var injury = new Color("b64c4c");

        var centerX = w * .5f;
        var floorY = h * .93f;
        var headRadius = infant ? h * .105f : child ? h * .085f : adolescent ? h * .073f : h * .068f;
        var bodyTop = infant ? h * .34f : child ? h * .27f : h * .22f;
        var bodyBottom = infant ? h * .64f : child ? h * .66f : h * .62f;
        var shoulder = (float)(w * (.16 + genome.BodyFrameWidth * .08));
        var hip = (float)(w * (.11 + genome.BodyAdipositySetPoint * .075));
        if (npc.Identity.Sex == Sex.Female) hip *= 1.08f;
        var torsoBulge = (float)(1 + genome.BodyAdipositySetPoint * .18);
        shoulder *= torsoBulge;

        var headCenter = new Vector2(centerX, bodyTop - headRadius * 1.33f);
        var neckY = headCenter.Y + headRadius * .92f;

        // Soft shadow anchors the figure and makes the card feel less diagrammatic.
        DrawSetTransform(new Vector2(centerX, floorY), 0, new Vector2(1.45f, .30f));
        DrawCircle(Vector2.Zero, Math.Max(13, w * .14f), new Color(0, 0, 0, .10f));
        DrawSetTransform(Vector2.Zero, 0, Vector2.One);

        // Legs. Missing distal segments are omitted for permanent amputations.
        var legStartY = bodyBottom - 2;
        var kneeY = infant ? h * .74f : h * .76f;
        var ankleY = infant ? h * .82f : h * .89f;
        var legGap = Math.Max(7, hip * .43f);
        DrawLeg(BodySide.Left, centerX - legGap, legStartY, kneeY, ankleY, floorY, clothing, skin, outline);
        DrawLeg(BodySide.Right, centerX + legGap, legStartY, kneeY, ankleY, floorY, clothing, skin, outline);

        // Torso as a slightly tapered polygon, widened by inherited frame/adiposity.
        var torso = new PackedVector2Array(new[]
        {
            new Vector2(centerX - shoulder, bodyTop),
            new Vector2(centerX + shoulder, bodyTop),
            new Vector2(centerX + hip, bodyBottom),
            new Vector2(centerX - hip, bodyBottom)
        });
        DrawColoredPolygon(torso, clothing);
        DrawPolyline(new PackedVector2Array(new[] { torso[0], torso[1], torso[2], torso[3], torso[0] }), outline, compact ? 1.2f : 2f, true);

        // Sash gives a recognisable Murim silhouette without hard-coding social rank.
        var sashY = Mathf.Lerp(bodyTop, bodyBottom, .62f);
        DrawLine(new Vector2(centerX - hip, sashY), new Vector2(centerX + hip, sashY), Lighten(clothing, .18f), compact ? 2 : 4, true);

        // Arms and hands.
        var shoulderY = bodyTop + (bodyBottom - bodyTop) * .12f;
        DrawArm(BodySide.Left, centerX - shoulder, shoulderY, centerX - shoulder * 1.42f, h * .48f, centerX - shoulder * 1.30f, h * .64f, clothing, skin, outline);
        DrawArm(BodySide.Right, centerX + shoulder, shoulderY, centerX + shoulder * 1.42f, h * .48f, centerX + shoulder * 1.30f, h * .64f, clothing, skin, outline);

        // Neck.
        DrawLine(new Vector2(centerX, neckY - 1), new Vector2(centerX, bodyTop + 2), skin, Math.Max(5, headRadius * .38f), true);

        // Head: scale a circle into a face shape using inherited face width and growth stage.
        var faceWidthScale = (float)(.80 + genome.FaceWidth * .28 + (infant ? genome.ChildhoodRoundness * .12 : 0));
        var faceHeightScale = (float)(1.04 + genome.JawLength * .11 + (adolescent ? genome.AdolescentLengthening * .08 : 0));
        DrawSetTransform(headCenter, 0, new Vector2(faceWidthScale, faceHeightScale));
        DrawCircle(Vector2.Zero, headRadius, skin);
        DrawArc(Vector2.Zero, headRadius, 0, Mathf.Tau, 32, outline, compact ? 1 : 1.5f, true);
        DrawSetTransform(Vector2.Zero, 0, Vector2.One);

        DrawHair(headCenter, headRadius, hair, genome.HairDensity, elder);
        if (!compact) DrawFace(headCenter, headRadius, skin, hair);
        DrawInjuryMarkers(injury, centerX, headCenter, headRadius, bodyTop, bodyBottom, shoulder, hip, h);
    }

    private void DrawArm(BodySide side, float sx, float sy, float ex, float ey, float wx, float wy, Color clothing, Color skin, Color outline)
    {
        if (npc is null) return;
        var upperMissing = IsAmputated(side, BodyRegion.UpperArm);
        if (upperMissing) { DrawAmputationEnd(new Vector2(sx, sy), clothing); return; }

        DrawLine(new Vector2(sx, sy), new Vector2(ex, ey), outline, compact ? 6 : 13, true);
        DrawLine(new Vector2(sx, sy), new Vector2(ex, ey), clothing, compact ? 4 : 10, true);
        var forearmMissing = IsAmputated(side, BodyRegion.Forearm);
        if (forearmMissing) { DrawAmputationEnd(new Vector2(ex, ey), skin); return; }

        DrawLine(new Vector2(ex, ey), new Vector2(wx, wy), skin, compact ? 4 : 9, true);
        var handMissing = IsAmputated(side, BodyRegion.Hand) || IsAmputated(side, BodyRegion.Fingers);
        if (!handMissing) DrawCircle(new Vector2(wx, wy), compact ? 3.2f : 6.2f, skin);
        else DrawAmputationEnd(new Vector2(wx, wy), skin);
    }

    private void DrawLeg(BodySide side, float x, float startY, float kneeY, float ankleY, float floorY, Color clothing, Color skin, Color outline)
    {
        var sign = side == BodySide.Left ? -1f : 1f;
        if (IsAmputated(side, BodyRegion.Thigh)) { DrawAmputationEnd(new Vector2(x, startY + 12), clothing); return; }
        var knee = new Vector2(x + sign * 2, kneeY);
        DrawLine(new Vector2(x, startY), knee, outline, compact ? 7 : 15, true);
        DrawLine(new Vector2(x, startY), knee, clothing, compact ? 5 : 12, true);
        if (IsAmputated(side, BodyRegion.LowerLeg)) { DrawAmputationEnd(knee, skin); return; }
        var ankle = new Vector2(x - sign * 1.5f, ankleY);
        DrawLine(knee, ankle, outline, compact ? 5 : 11, true);
        DrawLine(knee, ankle, skin, compact ? 3.5f : 8, true);
        if (IsAmputated(side, BodyRegion.Foot) || IsAmputated(side, BodyRegion.Toes)) { DrawAmputationEnd(ankle, skin); return; }
        DrawLine(ankle, new Vector2(x + sign * (compact ? 6 : 11), floorY), outline, compact ? 4 : 8, true);
        DrawLine(ankle, new Vector2(x + sign * (compact ? 6 : 11), floorY), Darken(clothing, .18f), compact ? 2.7f : 6, true);
    }

    private void DrawHair(Vector2 head, float r, Color hair, double density, bool elder)
    {
        var capRadius = r * (float)(.88 + density * .15);
        DrawSetTransform(new Vector2(head.X, head.Y - r * .20f), 0, new Vector2(1.05f, .62f));
        DrawCircle(Vector2.Zero, capRadius, hair);
        DrawSetTransform(Vector2.Zero, 0, Vector2.One);
        if (density > .38 && !compact)
        {
            var length = r * (float)(.35 + density * .55);
            DrawLine(new Vector2(head.X - r * .78f, head.Y - r * .08f), new Vector2(head.X - r * .72f, head.Y + length), hair, Math.Max(3, r * .18f), true);
            DrawLine(new Vector2(head.X + r * .78f, head.Y - r * .08f), new Vector2(head.X + r * .72f, head.Y + length), hair, Math.Max(3, r * .18f), true);
        }
    }

    private void DrawFace(Vector2 head, float r, Color skin, Color hair)
    {
        if (npc is null || genome is null || appearance is null) return;
        var asym = (float)genome.FacialAsymmetry * r * .22f;
        var eyeY = head.Y - r * .06f;
        var eyeGap = r * (float)(.28 + genome.EyeSpacing * .16);
        var eyeR = r * (float)(.055 + genome.EyeSize * .035);
        var eyeColor = Color.Lerp(new Color("473b32"), new Color("74858b"), (float)genome.EyePigment * .35f);
        DrawCircle(new Vector2(head.X - eyeGap - asym, eyeY), eyeR, eyeColor);
        DrawCircle(new Vector2(head.X + eyeGap + asym * .35f, eyeY + asym * .10f), eyeR, eyeColor);
        var browY = eyeY - r * .16f;
        DrawLine(new Vector2(head.X - eyeGap - r * .12f, browY), new Vector2(head.X - eyeGap + r * .12f, browY - r * .025f), hair, 2, true);
        DrawLine(new Vector2(head.X + eyeGap - r * .12f, browY - r * .02f), new Vector2(head.X + eyeGap + r * .12f, browY), hair, 2, true);
        var noseEnd = new Vector2(head.X + asym * .18f, head.Y + r * (float)(.12 + genome.NoseLength * .16));
        DrawLine(new Vector2(head.X, head.Y), noseEnd, Darken(skin, .20f), 1.4f, true);
        var mouthY = head.Y + r * .42f;
        var mouthW = r * (float)(.18 + genome.LipFullness * .20);
        DrawLine(new Vector2(head.X - mouthW, mouthY), new Vector2(head.X + mouthW, mouthY + asym * .05f), new Color("9a6460"), 2, true);
        if (appearance.EyeBagAmount > .38)
        {
            var bag = new Color(0.25f, 0.23f, 0.28f, (float)Math.Clamp(appearance.EyeBagAmount * .28, 0, .28));
            DrawArc(new Vector2(head.X - eyeGap, eyeY + r * .09f), r * .14f, .15f, 2.95f, 12, bag, 2, true);
            DrawArc(new Vector2(head.X + eyeGap, eyeY + r * .09f), r * .14f, .15f, 2.95f, 12, bag, 2, true);
        }
    }

    private void DrawInjuryMarkers(Color color, float centerX, Vector2 head, float r, float bodyTop, float bodyBottom, float shoulder, float hip, float h)
    {
        if (npc is null) return;
        foreach (var record in npc.Injuries.Where(i => i.Active || i.Permanent).Take(8))
        {
            var xSide = record.Side == BodySide.Left ? -1f : record.Side == BodySide.Right ? 1f : 0f;
            var p = record.Region switch
            {
                BodyRegion.Head or BodyRegion.Face or BodyRegion.Eye or BodyRegion.Ear => head + new Vector2(xSide * r * .46f, 0),
                BodyRegion.Neck => new Vector2(centerX, bodyTop - 5),
                BodyRegion.Chest => new Vector2(centerX + xSide * shoulder * .35f, Mathf.Lerp(bodyTop, bodyBottom, .30f)),
                BodyRegion.Abdomen or BodyRegion.Dantian or BodyRegion.InternalOrgans => new Vector2(centerX, Mathf.Lerp(bodyTop, bodyBottom, .66f)),
                BodyRegion.Shoulder or BodyRegion.UpperArm => new Vector2(centerX + xSide * shoulder * 1.16f, bodyTop + 15),
                BodyRegion.Elbow or BodyRegion.Forearm or BodyRegion.Wrist or BodyRegion.Hand or BodyRegion.Fingers => new Vector2(centerX + xSide * shoulder * 1.35f, h * .56f),
                BodyRegion.Hip or BodyRegion.Thigh => new Vector2(centerX + xSide * hip * .75f, h * .66f),
                BodyRegion.Knee => new Vector2(centerX + xSide * hip * .50f, h * .76f),
                BodyRegion.LowerLeg or BodyRegion.Ankle or BodyRegion.Foot or BodyRegion.Toes => new Vector2(centerX + xSide * hip * .45f, h * .88f),
                _ => new Vector2(centerX, Mathf.Lerp(bodyTop, bodyBottom, .48f))
            };
            var radius = compact ? 2.5f : 5f + (int)record.Severity * .5f;
            DrawCircle(p, radius, color);
            if (record.Permanent) DrawArc(p, radius + 3, 0, Mathf.Tau, 18, new Color("7b1f2b"), 2, true);
        }
    }

    private bool IsAmputated(BodySide side, BodyRegion region)
        => npc?.Injuries.Any(i => i.Permanent && i.Kind == InjuryKind.Amputation && i.Side == side && i.Region == region) == true;

    private void DrawAmputationEnd(Vector2 p, Color color)
    {
        DrawCircle(p, compact ? 3 : 6, Darken(color, .18f));
        DrawArc(p, compact ? 4 : 8, 0, Mathf.Tau, 16, new Color("8f4a45"), compact ? 1 : 2, true);
    }

    private static Color SkinColor(double tone, double pallor)
    {
        var light = new Color("f2d6bd");
        var medium = new Color("c9906e");
        var deep = new Color("80533f");
        var baseColor = tone < .52 ? Color.Lerp(light, medium, (float)(tone / .52)) : Color.Lerp(medium, deep, (float)((tone - .52) / .48));
        return Color.Lerp(baseColor, new Color("e7dfda"), (float)Math.Clamp(pallor * .42, 0, .42));
    }

    private static Color HairColor(double pigment, double grey)
    {
        var dark = Color.Lerp(new Color("191716"), new Color("4b362d"), (float)Math.Clamp(pigment * .45, 0, .45));
        return Color.Lerp(dark, new Color("b7b7b2"), (float)Math.Clamp(grey, 0, 1));
    }

    private static Color ClothingColor(Guid id)
    {
        var bytes = id.ToByteArray();
        var palette = new[] { new Color("526b72"), new Color("6f6456"), new Color("765d69"), new Color("49665a"), new Color("6a7050"), new Color("555f7a") };
        return palette[bytes[0] % palette.Length];
    }

    private static Color Darken(Color c, float amount) => c.Darkened(amount);
    private static Color Lighten(Color c, float amount) => c.Lightened(amount);

    // Tiny proxy used only to keep visual age logic self-contained; real gameplay age still comes from WorldClock.
    private sealed class WorldClockProxy : WorldClock
    {
        public WorldClockProxy(long birthDay, double apparentAge) { }
    }
}
