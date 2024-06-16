using Microsoft.Xna.Framework.Graphics;
using SpaceShared.UI;

namespace RadialMenu.Gmcm;

internal class HiddenElement : Element
{
    internal Element OriginalElement => originalElement;

    public override int Width => 0;

    public override int Height => 0;

    private readonly Element originalElement;

    public HiddenElement(Element originalElement)
    {
        this.originalElement = originalElement;
        LocalPosition = originalElement.LocalPosition;
    }

    public override void Draw(SpriteBatch b)
    {
    }

    public override void Update(bool isOffScreen = false)
    {
    }
}
