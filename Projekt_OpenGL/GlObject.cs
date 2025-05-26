using Silk.NET.OpenGL;

public class GlObject
{
    public uint? AlbedoTexId { get; init; }
    public uint? NormalTexId { get; init; }

    public uint Vao { get; }
    public uint Vertices { get; }
    public uint Colors { get; }
    public uint Indices { get; }
    public uint IndexArrayLength { get; }

    private GL Gl;

    public GlObject(uint vao, uint vertices, uint colors, uint indices,
                    uint indexArrayLength, GL gl,
                    uint? albedo = null, uint? normal = null)
    {
        Vao = vao;
        Vertices = vertices;
        Colors = colors;
        Indices = indices;
        IndexArrayLength = indexArrayLength;
        Gl = gl;

        AlbedoTexId = albedo;
        NormalTexId = normal;
    }

    public virtual void BindTextures(GL gl)
    {
        if (AlbedoTexId.HasValue)
        {
            gl.ActiveTexture(TextureUnit.Texture0);
            gl.BindTexture(TextureTarget.Texture2D, AlbedoTexId.Value);
        }

        if (NormalTexId.HasValue)
        {
            gl.ActiveTexture(TextureUnit.Texture1);
            gl.BindTexture(TextureTarget.Texture2D, NormalTexId.Value);
        }
    }

    internal void ReleaseGlObject()
    {
        Gl.DeleteBuffer(Vertices);
        Gl.DeleteBuffer(Colors);
        Gl.DeleteBuffer(Indices);
        Gl.DeleteVertexArray(Vao);
    }
}
