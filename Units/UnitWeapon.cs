namespace TenKingdoms;

public class UnitWeapon : Unit
{
    public override void DrawDetails(IRenderer renderer)
    {
        renderer.DrawWeaponDetails(this);
    }

    public override void HandleDetailsInput(IRenderer renderer)
    {
        renderer.HandleWeaponDetailsInput(this);
    }
}