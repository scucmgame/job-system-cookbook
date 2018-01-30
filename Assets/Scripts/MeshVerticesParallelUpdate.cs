﻿using UnityEngine;
using Unity.Collections;
using Unity.Jobs;

public class MeshVerticesParallelUpdate : MonoBehaviour
{
    [Range(0.05f, 1f)]
    [SerializeField]
    protected float m_Strength = 0.25f;

    NativeArray<Vector3> m_Vertices;

    CalculateJob m_CalculateJob;

    JobHandle m_JobHandle;

    MeshFilter m_MeshFilter;
    Mesh m_Mesh;

    protected void Start()
    {
        m_MeshFilter = gameObject.GetComponent<MeshFilter>();
        m_Mesh = m_MeshFilter.mesh;
        m_Mesh.MarkDynamic();
        
        // this persistent memory setup assumes our vertex count will not expand
        m_Vertices = new NativeArray<Vector3>(m_Mesh.vertices, Allocator.Persistent);
    }

    struct CalculateJob : IJobParallelFor
    {
        public NativeArray<Vector3> vertices;

        public float time;

        public float strength;

        public void Execute(int i)
        {
            var vertex = vertices[i];
            var perlin = Mathf.PerlinNoise(vertex.z, vertex.y) * strength;
            var noise = Vector3.one * perlin;
            var sine = Vector3.one * Mathf.Sin(time) * strength;

            vertex = vertex - sine + noise;

            vertices[i] = vertex;
        }
    }

    public void LateUpdate()
    {
        m_JobHandle.Complete();

        m_Mesh.vertices = m_CalculateJob.vertices.ToArray();
    }

    public void Update()
    {
        m_CalculateJob = new CalculateJob()
        {
            vertices = m_Vertices,
            time = Time.time,
            strength = m_Strength / 5f  // map .05-1 range to smaller real strength
        };

        m_JobHandle = m_CalculateJob.Schedule(m_Vertices.Length, 64);
    }

    private void OnDestroy()
    {
        m_Vertices.Dispose();
    }
}