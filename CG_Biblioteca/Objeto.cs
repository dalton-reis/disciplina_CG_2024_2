using OpenTK.Graphics.OpenGL4;
using System;
using System.Collections.Generic;

namespace CG_Biblioteca
{
  public class Objeto  //TODO: deveria ser uma class abstract ..??
  {
    // Objeto _______________________________________
    private readonly char rotulo;
    public char Rotulo { get => rotulo; }
    protected Objeto paiRef;
    private readonly List<Objeto> objetosLista = new List<Objeto>();
    private PrimitiveType primitivaTipo = PrimitiveType.LineLoop;
    public PrimitiveType PrimitivaTipo { get => primitivaTipo; set => primitivaTipo = value; }
    private float primitivaTamanho = 1;
    public float PrimitivaTamanho { get => primitivaTamanho; set => primitivaTamanho = value; }
    private Shader _shaderObjeto = new("Shaders/shader.vert", "Shaders/shaderBranca.frag");
    public Shader ShaderObjeto { set => _shaderObjeto = value; }

    // Vértices do objeto TODO: o objeto mundo deveria ter estes atributos abaixo?
    protected List<Ponto4D> pontosLista = [];
    public int PontosListaTamanho { get => pontosLista.Count; }
    private int _vertexBufferObject;
    private int _vertexArrayObject;

    // BBox do objeto _______________________________
    private readonly BBox bBox = new();
    public BBox Bbox()  //TODO: readonly
    {
      return bBox;
    }

    // Transformações do objeto _____________________
    private Transformacao4D matriz = new();
    private static Transformacao4D matrizGlobal = new();


    public Objeto(Objeto _paiRef, ref char _rotulo, Objeto objetoFilho = null)
    {
      this.paiRef = _paiRef;
      rotulo = _rotulo = Utilitario.CharProximo(_rotulo);
      if (_paiRef != null)
      {
        ObjetoAdicionar(objetoFilho);
      }
    }

    private void ObjetoAdicionar(Objeto objetoFilho)
    {
      if (objetoFilho == null)
      {
        paiRef.objetosLista.Add(this);
      }
      else
      {
        paiRef.FilhoAdicionar(objetoFilho);
      }
    }

    public void ObjetoAtualizar()
    {
      float[] vertices = new float[pontosLista.Count * 3];
      int ptoLista = 0;
      for (int i = 0; i < vertices.Length; i += 3)
      {
        vertices[i] = (float)pontosLista[ptoLista].X;
        vertices[i + 1] = (float)pontosLista[ptoLista].Y;
        vertices[i + 2] = (float)pontosLista[ptoLista].Z;
        ptoLista++;
      }

      GL.PointSize(primitivaTamanho);

      _vertexBufferObject = GL.GenBuffer();
      GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexBufferObject);
      GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.StaticDraw);
      _vertexArrayObject = GL.GenVertexArray();
      GL.BindVertexArray(_vertexArrayObject);
      GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
      GL.EnableVertexAttribArray(0);
    }

    public Transformacao4D ObjetoMatrizGlobal(Transformacao4D matrizGlobalPai)
    {
      if (rotulo != '@')
      {
        matrizGlobalPai = paiRef.matriz.MultiplicarMatriz(matrizGlobalPai);
        matrizGlobalPai = paiRef.ObjetoMatrizGlobal(matrizGlobalPai);
      }
      return matrizGlobalPai;
    }

    public void Desenhar(Transformacao4D matrizGrafo, Objeto objetoSelecionado)
    {
#if CG_OpenGL
      GL.PointSize(primitivaTamanho);

      GL.BindVertexArray(_vertexArrayObject);

      if (paiRef != null)
      {
        // ## 14. Grafo de cena: transformação
        // Considere a transformação global ao transformar (translação/escala/rotação) um polígono “pai”.  
        matrizGrafo = matrizGrafo.MultiplicarMatriz(matriz);
        _shaderObjeto.SetMatrix4("transform", matrizGrafo.ObterDadosOpenTK());
        _shaderObjeto.Use();
        GL.DrawArrays(primitivaTipo, 0, pontosLista.Count);
        if (objetoSelecionado == this)
        {
          matrizGlobal = ObjetoMatrizGlobal(matriz);
          bBox.Atualizar(matrizGlobal, pontosLista);
#if CG_Gizmo && CG_BBox
          bBox.Desenhar();
#endif
        }
      }
      for (var i = 0; i < objetosLista.Count; i++)
      {
        objetosLista[i].Desenhar(matrizGrafo, objetoSelecionado);
      }
#endif
    }

    #region Objeto: CRUD

    public void FilhoAdicionar(Objeto filho)
    {
      this.objetosLista.Add(filho);
    }
    public Ponto4D PontosId(int id)
    {
      return pontosLista[id];
    }

    public void PontosAdicionar(Ponto4D pto)
    {
      pontosLista.Add(pto);
      ObjetoAtualizar();
    }

    public void PontosAlterar(Ponto4D pto, int posicao)
    {
      pontosLista[posicao] = pto;
      ObjetoAtualizar();
    }

    public void PontosApagar()
    {
      pontosLista.Clear();
      ObjetoAtualizar();
    }

    #endregion

    public Ponto4D MatrizGlobalInversa(Ponto4D mousePto)
    {
      matrizGlobal = ObjetoMatrizGlobal(matriz);    // Atualiza a matrizGlobal

      matrizGlobal.Inversa();
      return matrizGlobal.MultiplicarPonto(mousePto);
    }

    #region Objeto: Grafo de Cena
    //TODO: estes métodos não deveriam estar na classe GrafoCena da CG_Biblioteca?

    public Objeto GrafocenaBusca(char _rotulo)
    {
      if (rotulo == _rotulo)
      {
        return this;
      }
      foreach (var objeto in objetosLista)
      {
        var obj = objeto.GrafocenaBusca(_rotulo);
        if (obj != null)
        {
          return obj;
        }
      }
      return null;
    }

    public Dictionary<char, Objeto> GrafocenaAtualizar(Dictionary<char, Objeto> lista)
    {
      lista.Add(rotulo, this);
      foreach (var objeto in objetosLista)
      {
        lista = objeto.GrafocenaAtualizar(lista);
      }
      return lista;
    }

    public void GrafocenaImprimir(string idt)
    {
      Console.WriteLine(idt + rotulo);
      foreach (var objeto in objetosLista)
      {
        objeto.GrafocenaImprimir(idt + "  ");
      }
    }

    #endregion

    public void OnUnload()
    {
      foreach (var objeto in objetosLista)
      {
        objeto.OnUnload();
      }

      GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
      GL.BindVertexArray(0);
      GL.UseProgram(0);

      GL.DeleteBuffer(_vertexBufferObject);
      GL.DeleteVertexArray(_vertexArrayObject);

      // GL.DeleteProgram(_shaderObjeto.Handle);
    }

#if CG_Debug
    protected string ImprimeToString()
    {
      string retorno;
      retorno = "__ Objeto Original: " + rotulo + "\n";
      for (var i = 0; i < pontosLista.Count; i++)
      {
        retorno += "P" + i + "[ " +
        string.Format("{0,10}", pontosLista[i].X) + " | " +
        string.Format("{0,10}", pontosLista[i].Y) + " | " +
        string.Format("{0,10}", pontosLista[i].Z) + " | " +
        string.Format("{0,10}", pontosLista[i].W) + " ]" + "\n";
      }

      retorno += "__ Objeto Transformado: " + rotulo + "\n";
      for (var i = 0; i < pontosLista.Count; i++)
      {
        retorno += "P" + i + "[ " +
        string.Format("{0,10}", pontosLista[i].X) + " | " +
        string.Format("{0,10}", pontosLista[i].Y) + " | " +
        string.Format("{0,10}", pontosLista[i].Z) + " | " +
        string.Format("{0,10}", pontosLista[i].W) + " ]" + "\n";
      }

      bBox.Atualizar(ObjetoMatrizGlobal(matriz), pontosLista);
      retorno += bBox.ToString();
      return retorno;
    }
#endif

  }
}