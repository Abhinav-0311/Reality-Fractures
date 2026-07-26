using UnityEngine;
using System.Collections;

namespace RealityFractures
{
    public static class TemporalVFXHelper
    {
        private class WaveAnimation : MonoBehaviour
        {
            public Color color;
            public float duration = 0.6f;
            public float maxRadius = 0.45f;

            private float elapsed = 0f;
            private Renderer rend;
            private Material mat;

            private void Start()
            {
                rend = GetComponent<Renderer>();
                if (rend != null)
                {
                    mat = rend.material;
                }
            }

            private void Update()
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                // Expand radius
                float currentRadius = Mathf.Lerp(0.05f, maxRadius, Mathf.Sin(t * Mathf.PI * 0.5f));
                transform.localScale = new Vector3(currentRadius, 0.002f, currentRadius);

                // Fade alpha
                if (mat != null)
                {
                    float alpha = Mathf.Lerp(0.7f, 0f, t);
                    if (mat.HasProperty("_Color"))
                    {
                        mat.SetColor("_Color", new Color(color.r, color.g, color.b, alpha));
                    }
                }

                if (t >= 1f)
                {
                    Destroy(gameObject);
                }
            }
        }

        public static void SpawnEnergyWave(Vector3 position, Color waveColor)
        {
            GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = "TemporalEnergyWave";
            ring.transform.position = position + Vector3.up * 0.01f; // Just above platform
            ring.transform.localScale = new Vector3(0.05f, 0.002f, 0.05f);

            Collider col = ring.GetComponent<Collider>();
            if (col != null) Object.Destroy(col);

            Renderer r = ring.GetComponent<Renderer>();
            if (r != null)
            {
                Shader s = Shader.Find("Sprites/Default");
                if (s == null) s = Shader.Find("Unlit/Color");
                Material m = new Material(s);
                m.color = new Color(waveColor.r, waveColor.g, waveColor.b, 0.7f);
                r.material = m;
            }

            var anim = ring.AddComponent<WaveAnimation>();
            anim.color = waveColor;
        }

        public static void SpawnParticleBurst(Vector3 position, Color primaryColor, int count = 25)
        {
            GameObject fxObj = new GameObject("TemporalParticleBurst");
            fxObj.transform.position = position;

            ParticleSystem ps = fxObj.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.duration = 1f;
            main.loop = false;
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.3f, 0.8f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.01f, 0.025f);
            main.startColor = primaryColor;
            main.startLifetime = 0.8f;
            main.gravityModifier = -0.1f; // Float slightly upward

            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.1f;

            ps.Play();
            Object.Destroy(fxObj, 1.5f);
        }

        public static void SpawnRealityFireworks(Vector3 position)
        {
            SpawnParticleBurst(position + Vector3.up * 0.15f, new Color(0.95f, 0.75f, 0.2f), 40); // Amber
            SpawnParticleBurst(position + Vector3.up * 0.20f, new Color(0.2f, 0.95f, 0.5f), 40);  // Emerald
            SpawnParticleBurst(position + Vector3.up * 0.25f, new Color(0.2f, 0.75f, 1.0f), 40);  // Azure
            SpawnEnergyWave(position, Color.white);
        }
    }
}
