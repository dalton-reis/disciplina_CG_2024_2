#define CG_DEBUG
#define CG_Gizmo      
#define CG_OpenGL      
// #define CG_OpenTK
// #define CG_DirectX      
// #define CG_Privado  

//FIXME: as diretivas parecem não funcionar direito. Aqui neste projeto sim, mas quando estão na CG_Biblioteca não funciona.

using CG_Biblioteca;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenTK.Windowing.Desktop;
using System;
using System.Collections.Generic;
using OpenTK.Mathematics;

namespace gcgcg
{
  public class Mundo : GameWindow
  {
    private static Objeto mundo = null;

    private char rotuloAtual = '?';
    private Dictionary<char, Objeto> grafoLista = [];
    private Objeto objetoSelecionado = null;

    private readonly float[] _sruEixos =
    [
       0.0f,  0.0f,  0.0f, /* X- */      0.5f,  0.0f,  0.0f, /* X+ */
       0.0f,  0.0f,  0.0f, /* Y- */      0.0f,  0.5f,  0.0f, /* Y+ */
       0.0f,  0.0f,  0.0f, /* Z- */      0.0f,  0.0f,  0.5f  /* Z+ */
    ];

    private int _vertexBufferObject_sruEixos;
    private int _vertexArrayObject_sruEixos;

    private Shader _shaderVermelha;
    private Shader _shaderVerde;
    private Shader _shaderAzul;
    private Shader _shaderCiano;

    private bool mouseMovtoPrimeiro = true;
    private Ponto4D mouseMovtoUltimo;

    public Mundo(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings)
      : base(gameWindowSettings, nativeWindowSettings)
    {
      mundo ??= new Objeto(null, ref rotuloAtual); //padrão Singleton
    }

    private void GrafocenaAtualizar()
    {
      grafoLista.Clear();
      grafoLista = mundo.GrafocenaAtualizar(grafoLista);
    }

    private bool GrafoCenaProximo()
    {
      GrafocenaAtualizar();
      var itGrafo = grafoLista.GetEnumerator();
      itGrafo.MoveNext();
      itGrafo.MoveNext();
      if (objetoSelecionado == null)
      {
        objetoSelecionado = itGrafo.Current.Value;
        return false;
      }
      if (objetoSelecionado.Rotulo == '@')
      {
        objetoSelecionado = itGrafo.Current.Value;
        return true;
      }
      do
      {
        if (itGrafo.Current.Key == objetoSelecionado.Rotulo)
        {
          itGrafo.MoveNext();
          objetoSelecionado = itGrafo.Current.Value;
          return true;
        }
      } while (itGrafo.MoveNext());
      return false;
    }

    protected override void OnLoad()
    {
      base.OnLoad();

      Utilitario.Diretivas();
#if CG_DEBUG      
      Console.WriteLine("Tamanho interno da janela de desenho: " + ClientSize.X + "x" + ClientSize.Y);
#endif

      GL.ClearColor(0.0f, 0.0f, 0.0f, 1.0f);

      #region Cores
      _shaderVermelha = new Shader("Shaders/shader.vert", "Shaders/shaderVermelha.frag");
      _shaderVerde = new Shader("Shaders/shader.vert", "Shaders/shaderVerde.frag");
      _shaderAzul = new Shader("Shaders/shader.vert", "Shaders/shaderAzul.frag");
      _shaderCiano = new Shader("Shaders/shader.vert", "Shaders/shaderCiano.frag");
      #endregion

      #region Eixos: SRU  
      _vertexBufferObject_sruEixos = GL.GenBuffer();
      GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexBufferObject_sruEixos);
      GL.BufferData(BufferTarget.ArrayBuffer, _sruEixos.Length * sizeof(float), _sruEixos, BufferUsageHint.StaticDraw);
      _vertexArrayObject_sruEixos = GL.GenVertexArray();
      GL.BindVertexArray(_vertexArrayObject_sruEixos);
      GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
      GL.EnableVertexAttribArray(0);
      #endregion

      #region Objeto: polígono qualquer  
      List<Ponto4D> pontosPoligono =
      [
        new Ponto4D(0.25, 0.25),
        new Ponto4D(0.75, 0.25),
        new Ponto4D(0.75, 0.75),
        new Ponto4D(0.50, 0.50),
        new Ponto4D(0.25, 0.75),
      ];
      objetoSelecionado = new Poligono(mundo, ref rotuloAtual, pontosPoligono);
      #endregion
      #region NÃO USAR: declara um objeto filho ao polígono
      objetoSelecionado = new Ponto(objetoSelecionado, ref rotuloAtual, new Ponto4D(0.50, 0.75));
      #endregion
      #region Objeto: retângulo  
      objetoSelecionado = new Retangulo(mundo, ref rotuloAtual, new Ponto4D(-0.25, 0.25), new Ponto4D(-0.75, 0.75))
      {
        PrimitivaTipo = PrimitiveType.LineLoop
      };
      #endregion
      #region Objeto: segmento de reta  
      objetoSelecionado = new SegReta(mundo, ref rotuloAtual, new Ponto4D(-0.25, -0.25), new Ponto4D(-0.75, -0.75));
      #endregion
      #region Objeto: ponto  
      objetoSelecionado = new Ponto(mundo, ref rotuloAtual, new Ponto4D(0.25, -0.25))
      {
        PrimitivaTipo = PrimitiveType.Points,
        PrimitivaTamanho = 10
      };
      #endregion

#if CG_Privado
      #region Objeto: circulo - origem
      objetoSelecionado = new Circulo(mundo, ref rotuloAtual, 0.2)
      {
        ShaderObjeto = new Shader("Shaders/shader.vert", "Shaders/shaderAmarela.frag")
      };
      #endregion
      #region Objeto: circulo
      objetoSelecionado = new Circulo(mundo, ref rotuloAtual, 0.1, new Ponto4D(0.0, -0.5))
      {
        ShaderObjeto = new Shader("Shaders/shader.vert", "Shaders/shaderAmarela.frag")
      };
      #endregion

      #region Objeto: SrPalito  
      objetoSelecionado = new SrPalito(mundo, ref rotuloAtual);
      #endregion

      #region Objeto: SplineBezier
      objetoSelecionado = new SplineBezier(mundo, ref rotuloAtual);
      #endregion

      #region Objeto: SplineInter
      objetoSelecionado = new SplineInter(mundo, ref rotuloAtual);
      #endregion
#endif

    }

    protected override void OnRenderFrame(FrameEventArgs e)
    {
      base.OnRenderFrame(e);

      GL.Clear(ClearBufferMask.ColorBufferBit);

      mundo.Desenhar(new Transformacao4D(), objetoSelecionado);

#if CG_Gizmo
      Gizmo_Sru3D();
#endif
      SwapBuffers();
    }

    protected override void OnUpdateFrame(FrameEventArgs e)
    {
      base.OnUpdateFrame(e);

      #region Teclado
      var estadoTeclado = KeyboardState;
      if (estadoTeclado.IsKeyDown(Keys.Escape))
        Close();

      if (estadoTeclado.IsKeyPressed(Keys.Space))
        GrafoCenaProximo();
        
      if (estadoTeclado.IsKeyPressed(Keys.P) && objetoSelecionado != null)
      {
        Console.WriteLine(objetoSelecionado);
      }

      if (estadoTeclado.IsKeyPressed(Keys.C) && objetoSelecionado != null)
      {
        objetoSelecionado.ShaderObjeto = _shaderCiano;
      }

      if (estadoTeclado.IsKeyPressed(Keys.Right) && objetoSelecionado != null)
      {
        if (objetoSelecionado.PontosListaTamanho > 0)
        {
          objetoSelecionado.PontosAlterar(new Ponto4D(objetoSelecionado.PontosId(0).X + 0.005, objetoSelecionado.PontosId(0).Y, 0), 0);
          objetoSelecionado.ObjetoAtualizar();
        }
      }

      if (estadoTeclado.IsKeyPressed(Keys.R) && objetoSelecionado != null)
      {
        //FIXME: Spline limpa os pontos da Spline, mas não limpa pontos e poliedro de controle 
        objetoSelecionado.PontosApagar();
      }
      #endregion

      #region  Mouse
      int janelaLargura = ClientSize.X;
      int janelaAltura = ClientSize.Y;
      Ponto4D mousePonto = new(MousePosition.X, MousePosition.Y);
      Ponto4D sruPonto = Utilitario.NDC_TelaSRU(janelaLargura, janelaAltura, mousePonto);

      if (estadoTeclado.IsKeyDown(Keys.LeftShift))
      {
        if (mouseMovtoPrimeiro)
        {
          mouseMovtoUltimo = sruPonto;
          mouseMovtoPrimeiro = false;
        }
        else
        {
          var deltaX = sruPonto.X - mouseMovtoUltimo.X;
          var deltaY = sruPonto.Y - mouseMovtoUltimo.Y;
          mouseMovtoUltimo = sruPonto;

          objetoSelecionado.PontosAlterar(new Ponto4D(objetoSelecionado.PontosId(0).X + deltaX, objetoSelecionado.PontosId(0).Y + deltaY, 0), 0);
          objetoSelecionado.ObjetoAtualizar();
        }
      }
      if (estadoTeclado.IsKeyDown(Keys.LeftShift))
      {
        objetoSelecionado.PontosAlterar(sruPonto, 0);
        objetoSelecionado.ObjetoAtualizar();
      }

      #endregion
    }

    protected override void OnResize(ResizeEventArgs e)
    {
      base.OnResize(e);

#if CG_DEBUG      
      Console.WriteLine("Tamanho interno da janela de desenho: " + ClientSize.X + "x" + ClientSize.Y);
#endif
      GL.Viewport(0, 0, ClientSize.X, ClientSize.Y);
    }

    protected override void OnUnload()
    {
      mundo.OnUnload();

      GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
      GL.BindVertexArray(0);
      GL.UseProgram(0);

      GL.DeleteBuffer(_vertexBufferObject_sruEixos);
      GL.DeleteVertexArray(_vertexArrayObject_sruEixos);

      GL.DeleteProgram(_shaderVermelha.Handle);
      GL.DeleteProgram(_shaderVerde.Handle);
      GL.DeleteProgram(_shaderAzul.Handle);
      GL.DeleteProgram(_shaderCiano.Handle);

      base.OnUnload();
    }

#if CG_Gizmo
    private void Gizmo_Sru3D()
    {
#if CG_OpenGL && !CG_DirectX
      var transform = Matrix4.Identity;
      GL.BindVertexArray(_vertexArrayObject_sruEixos);
      // EixoX
      _shaderVermelha.SetMatrix4("transform", transform);
      _shaderVermelha.Use();
      GL.DrawArrays(PrimitiveType.Lines, 0, 2);
      // EixoY
      _shaderVerde.SetMatrix4("transform", transform);
      _shaderVerde.Use();
      GL.DrawArrays(PrimitiveType.Lines, 2, 2);
      // EixoZ
      _shaderAzul.SetMatrix4("transform", transform);
      _shaderAzul.Use();
      GL.DrawArrays(PrimitiveType.Lines, 4, 2);
#elif CG_DirectX && !CG_OpenGL
      Console.WriteLine(" .. Coloque aqui o seu código em DirectX");
#elif (CG_DirectX && CG_OpenGL) || (!CG_DirectX && !CG_OpenGL)
      Console.WriteLine(" .. ERRO de Render - escolha OpenGL ou DirectX !!");
#endif
    }
#endif

  }
}
