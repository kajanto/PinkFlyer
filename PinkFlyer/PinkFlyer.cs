using System;
using System.Collections.Generic;
using Jypeli;
using Jypeli.Assets;
using Jypeli.Controls;
using Jypeli.Effects;
using Jypeli.Widgets;

public class PinkFlyer : PhysicsGame
{
    Vector nopeusYlos = new Vector(0, 600);
    Vector nopeusAlas = new Vector(0, -600);
    Vector nopeusOikealle = new Vector(600, 0);
    Vector nopeusVasemmalle = new Vector(-600, 0);

    Image taustaKuva = LoadImage("background");
    Image pelaajahahmo = LoadImage("pink_guy_vaaka");
    Image vihulainen = LoadImage("almond1");
    Image ammus = LoadImage("cromosomi");

    PhysicsObject oikeaReuna;
    PhysicsObject pelaaja = new PhysicsObject(250.0, 75.0);
    PhysicsObject vihu;
    PhysicsObject panos;

    private static String[] taso0 = {
        "             *   ",
        "                 ",
        "                 ",
        "          *      ",
        "                 ",
        "                 ",
        "             *   " };

    private double tileWidth;
    private double tileHeight;

    private TileMap tiles;

    /// <summary>
    /// Paras peli ikinä!
    /// </summary>
    public override void Begin()
    {
        LuoKentta();
        SetKeyConfig();
    }

    /// <summary>
    /// Luodaan pelikenttä.
    /// </summary>
    void LuoKentta()
    {
        Level.Background.Image = taustaKuva;

        IsFullScreen = true;
        Level.Width = Screen.Width;
        Level.Height = Screen.Height;
        Level.CreateBorders(false);
        Camera.ZoomToLevel();

        oikeaReuna = Level.CreateRightBorder();
        oikeaReuna.Restitution = 0.0;
        oikeaReuna.IsVisible = false;

        tileWidth = Screen.Width / taso0[0].Length;
        tileHeight = Screen.Height / taso0.Length;
        tiles = TileMap.FromStringArray(taso0);

        tiles['*'] = LisaaManteli;
        tiles.Insert(tileWidth, tileHeight);

        pelaaja = LisaaPelaaja();
        Level.Background.ScaleToLevelFull();
    }

    /// <summary>
    /// Asetetaan näppäimet.
    /// </summary>
    void SetKeyConfig()
    {
        Keyboard.Listen(Key.Up, ButtonState.Down, AsetaNopeus, "Liikuta pelaajaa ylös", pelaaja, nopeusYlos);
        Keyboard.Listen(Key.Up, ButtonState.Released, AsetaNopeus, null, pelaaja, Vector.Zero);

        Keyboard.Listen(Key.Down, ButtonState.Down, AsetaNopeus, "Liikuta pelaajaa alas", pelaaja, nopeusAlas);
        Keyboard.Listen(Key.Down, ButtonState.Released, AsetaNopeus, null, pelaaja, Vector.Zero);

        Keyboard.Listen(Key.Left, ButtonState.Down, AsetaNopeus, "Liikuta pelaajaa ylös", pelaaja, nopeusVasemmalle);
        Keyboard.Listen(Key.Left, ButtonState.Released, AsetaNopeus, null, pelaaja, Vector.Zero);

        Keyboard.Listen(Key.Right, ButtonState.Down, AsetaNopeus, "Liikuta pelaajaa ylös", pelaaja, nopeusOikealle);
        Keyboard.Listen(Key.Right, ButtonState.Released, AsetaNopeus, null, pelaaja, Vector.Zero);

        Keyboard.Listen(Key.Space, ButtonState.Pressed, Ammu, "Ampuu");

        Keyboard.Listen(Key.F1, ButtonState.Pressed, ShowControlHelp, "Näytä näppäinohjeet");
        Keyboard.Listen(Key.Escape, ButtonState.Pressed, Exit, "Lopeta peli");
    }

    /// <summary>
    /// Luodaan pelaajan ampumat ammukset.
    /// </summary>
    void Ammu()
    {
        panos = new PhysicsObject(20, 15, Shape.Rectangle);
        panos.Position = pelaaja.Position + new Vector(125, 0);
        panos.Hit(new Vector(800, 0));
        AddCollisionHandler(panos, Tormays);
        panos.Image = ammus;
        Add(panos);
    }

    /// <summary>
    /// Käsittelee panosten törmäyksiä.
    /// </summary>
    /// <param name="panos">Pelaajan ampuma panos.</param>
    /// <param name="kohde">Asia mihin panos osuu.</param>
    public void Tormays(PhysicsObject panos, PhysicsObject kohde)
    {
        if (kohde == oikeaReuna)
        {
            panos.Destroy();
        }

        if (kohde.Tag.Equals("vihu"))
        {
            kohde.Destroy();
            panos.Destroy();
        }

    }


    /// <summary>
    /// Luodaan pelaajahahmo.
    /// </summary>
    /// <returns>Pelaajahahmo</returns>
    PhysicsObject LisaaPelaaja()
    {
        pelaaja = new PhysicsObject(250.0, 75.0);
        pelaaja.Shape = Shape.Rectangle;
        pelaaja.X = Level.Left + 200;
        pelaaja.Y = 0.0;
        pelaaja.Restitution = 0.0;
        pelaaja.Image = pelaajahahmo;
        pelaaja.CanRotate = false;

        Add(pelaaja);
        return pelaaja;
    }


    public PhysicsObject LisaaManteli()
    {
        vihu = new PhysicsObject(tileWidth, tileHeight);
        vihu.Shape = Shape.Circle;
        vihu.Restitution = 0.0;
        vihu.Image = vihulainen;
        vihu.CanRotate = false;
        vihu.MakeStatic();
        vihu.Tag = "vihu";
        //AddCollisionHandler(vihu, TormaysVihuun);
        Add(vihu);
        return vihu;
    }

    public void TormaysVihuun(PhysicsObject kuka, PhysicsObject mihin)
    {
        //if (mihin == panos)
        {
            kuka.Destroy();
            panos.Destroy();
        } 
    }

    /// <summary>
    /// Asettaa pelaajahahmolle nopeuden.
    /// </summary>
    /// <param name="pelaaja">Pelaajahahmo</param>
    /// <param name="nopeus">Liikkeen nopeus</param>
    void AsetaNopeus(PhysicsObject pelaaja, Vector nopeus)
    {
        if ((nopeus.Y > 0) && (pelaaja.Top > Level.Top))
        {
            pelaaja.Velocity = Vector.Zero;
            return;
        }

        if ((nopeus.Y < 0) && (pelaaja.Bottom < Level.Bottom))
        {
            pelaaja.Velocity = Vector.Zero;
            return;
        }

        pelaaja.Velocity = nopeus;
    }
}
