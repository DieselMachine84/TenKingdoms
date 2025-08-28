namespace TenKingdoms;

public class UnitHuman : Unit
{
    public override void DrawDetails(IRenderer renderer)
    {
        renderer.DrawHumanDetails(this);
    }

    public override void HandleDetailsInput(IRenderer renderer)
    {
        renderer.HandleHumanDetailsInput(this);
    }
}