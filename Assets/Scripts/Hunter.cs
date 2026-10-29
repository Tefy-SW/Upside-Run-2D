
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer), typeof(Animator))]
public class Hunter : MonoBehaviour
{
    public GameManager gameManager;
    public Jugador jugador;

    [Header("Aparicion al inicio")]
    [Min(0f)] public float segundosInicio = 2f;

    [Header("Persecucion")]
    [Min(.5f)] public float distanciaTrasGolpe = 1.8f;
    [Min(.5f)] public float segundosDeAmenaza = 3.5f;
    [Min(.1f)] public float velocidadAcercamiento = 4f;
    [Min(.1f)] public float velocidadRetirada = 3f;

    private SpriteRenderer imagen;
    private Animator animador;
    private Camera camara;

    private float tiempoCerca;
    private float mitadAncho;

    private int vidaAnterior;

    private bool inicioActivado = false;

    void Awake()
    {
        imagen = GetComponent<SpriteRenderer>();
        animador = GetComponent<Animator>();
        camara = Camera.main;

        vidaAnterior = jugador.vida;

        mitadAncho = imagen.sprite.bounds.extents.x
            * Mathf.Abs(transform.lossyScale.x);

        Ocultar();
    }

    float PosicionFuera()
    {
        float izquierda = camara.transform.position.x
            - camara.orthographicSize * camara.aspect;

        return izquierda - mitadAncho - .4f;
    }

    void Ocultar()
    {
        transform.position = new Vector3(
            PosicionFuera(),
            gameManager.alturaSuelo,
            0f
        );

        imagen.enabled = false;
        animador.speed = 0f;
    }

    void Update()
    {
        if (!gameManager.partidaIniciada)
        {
            vidaAnterior = jugador.vida;
            Ocultar();
            return;
        }
        if (!inicioActivado)
        {
            inicioActivado = true;
            tiempoCerca = segundosInicio;

            transform.position = new Vector3(
                jugador.transform.position.x - distanciaTrasGolpe,
                gameManager.alturaSuelo,
                0f
            );

            imagen.enabled = true;
        }

        if (jugador.vida < vidaAnterior)
        {
            tiempoCerca = segundosDeAmenaza;
        }

        vidaAnterior = jugador.vida;

        tiempoCerca = Mathf.Max(
            0f,
            tiempoCerca - Time.deltaTime
        );

        bool acercarse = tiempoCerca > 0f || jugador.sinVida;
      
        float destino = PosicionFuera();

        if (acercarse)
        {
            destino = jugador.transform.position.x
                - distanciaTrasGolpe;
        }

        if (jugador.sinVida)
        {
            destino = jugador.transform.position.x - .8f;
        }

        float velocidad = acercarse
            ? velocidadAcercamiento
            : velocidadRetirada;

        Vector3 posicion = transform.position;

        posicion.x = Mathf.MoveTowards(
            posicion.x,
            destino,
            velocidad * Time.deltaTime
        );

        posicion.y = gameManager.alturaSuelo;

        transform.position = posicion;

        imagen.enabled = acercarse ||
            Mathf.Abs(posicion.x - destino) > .05f;


        if (imagen.enabled && !gameManager.partidaTerminada)
        {
            animador.speed = 1f;
        }
        else
        {
            animador.speed = 0f;
        }
    }
}
