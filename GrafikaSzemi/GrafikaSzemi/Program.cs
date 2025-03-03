using Silk.NET.OpenGL;
using Silk.NET.Windowing;

namespace GrafikaSzemi
{
    internal class Program
    {
        private static IWindow grapichWindow;
        private static GL gl;
        static void Main(string[] args)
        {
            WindowOptions options = WindowOptions.Default;
            options.Title = "Grafika szeminarium";
            options.Size = new Silk.NET.Maths.Vector2D<int>(500, 500);

            grapichWindow = Window.Create(options);
         

            grapichWindow.Load += GrapichWindow_Load;
            grapichWindow.Update += GrapichWindow_Update;
            grapichWindow.Render += GrapichWindow_Render;
            grapichWindow.Closing += GrapichWindow_Closing;
            grapichWindow.Run();
        }

        private static void GrapichWindow_Load()
        {
            //Console.WriteLine("Loaded.");
            gl = grapichWindow.CreateOpenGL();
            gl.ClearColor(System.Drawing.Color.White);
        }

        private static void GrapichWindow_Closing()
        {
            //Console.WriteLine("$Closed");
        }
        //megjelenites
        private static void GrapichWindow_Render(double deltaTime)
        {
            //ide jonnek az OpenGL kodok
            //Console.WriteLine($"Render after {deltaTime} [s].");
            gl.Clear(ClearBufferMask.ColorBufferBit); //torlunk minden szint - minden pixelt ujra kell szamolni
        }

        //meg nem jelenitett objektumok frissitese a vilagban
        private static void GrapichWindow_Update(double deltaTime)
        {
            //ide nem kell OpenGL kodot irni
            //szalbiztos
            //Console.WriteLine($"Update after {deltaTime}[s].");
        }
    }
}