using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu] [System.Serializable]
public class LaserSurface : ScriptableObject
{
    public enum ReflectionType { termination, simpleReflection, angleRefraction } //Used to indicate surface properties as defined in PlayerController

    //Surface Properties:
    public float intensifiesLaser; //Whether or not hitting this surface will change laser intensity, and by how much
    public int magnifiesLaser; //Whether or not hitting this surface will change laser size, and by how much
    public float refractionAngle; //The angle this object refracts laser at, if angleRefraction is enabled

    //Surface Reflection Type:
    public ReflectionType reflectionProperties; //What effect this surface will have on laser trajectory
}
