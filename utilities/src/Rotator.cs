// Rotates the camera around the origin facing a target

using UnityEngine;
using UnityEngine.Rendering;

public class Rotator : MonoBehaviour
{

    // Radius
    [SerializeField]
    private float _radius = -.7f;

    [SerializeField]
    private float _speed;

    [SerializeField]
    private Transform _target;

    [SerializeField]
    private float _height = .5f;

    private void Update()
    {
        float x = _radius*Mathf.Cos(_speed*Time.time);
        float z = _radius*Mathf.Sin(_speed*Time.time);
        transform.position = new Vector3(x, _height, z);
        transform.LookAt(_target);
    }

}
