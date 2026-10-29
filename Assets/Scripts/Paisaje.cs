using UnityEngine;

public class Paisaje : MonoBehaviour
{
    [Min(0)] public float velocidad = 1f;
    [Min(.1f)] public float anchoPieza = 24f;

    void Update()
    {
        foreach (Transform pieza in transform)
        {
            pieza.localPosition += Vector3.left * velocidad * Time.deltaTime;
            if (pieza.localPosition.x < -anchoPieza * 1.5f)
            {
                float derecha = pieza.localPosition.x;
                foreach (Transform otra in transform)
                    derecha = Mathf.Max(derecha, otra.localPosition.x);
                Vector3 posicion = pieza.localPosition;
                posicion.x = derecha + anchoPieza;
                pieza.localPosition = posicion;
            }
        }
    }
}
