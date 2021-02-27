using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class LeafWindParticle : MonoBehaviour
{
    [SerializeField]
    GameRenderer gameRenderer;
    [SerializeField]
    ParticleSystem system;

    [SerializeField]
    float windResistance = 1f;
    [SerializeField]
    int maxAfloat = 4;

    [SerializeField]
    bool debugViewVelocity = false;

    private ParticleSystem.Particle[] particles = { };
    private List<Vector4> particleRandom = new List<Vector4>();
    private int numParticles;
    private HashSet<int> afloatIndices = new HashSet<int>();

    void LateUpdate()
    {
        if (particles.Length < system.main.maxParticles) {
            particles = new ParticleSystem.Particle[system.main.maxParticles];
        }

        // reinitialize random data if count changed
        {
            var customData = system.customData;
            customData.enabled = system.particleCount > numParticles;
        }

        numParticles = system.GetParticles(particles);
        int numVec = system.GetCustomParticleData(particleRandom, ParticleSystemCustomData.Custom1);
        if (numParticles != numVec) {
            Debug.LogError("Count mismatch");
        }

        float windSpeed = gameRenderer.windSampler._WindShiftSpeed;
        float windStrength = gameRenderer.windSampler._WindStrength;
        float effectiveWindResistance = windResistance / windSpeed;

        // Update particles according to flow
        for (int i = 0; i < numParticles; i++) {
            Vector3 velocity = particles[i].velocity;
            Vector3 facingVector = particleRandom[i];
            facingVector.Normalize();
            float counter = particleRandom[i].w;

            Vector3 wind = gameRenderer.windSampler.WindVelocity(particles[i].position);

            bool isParticleInAir = Mathf.Abs(velocity.y) > Mathf.Epsilon;
            if (!isParticleInAir && velocity.magnitude < 1f) {
                afloatIndices.Remove(i);
                velocity = Vector3.zero;
            }

            if (counter > 0) {
                counter -= Random.value * 0.01f * windSpeed;
            } else {
                counter = Random.value;

                if (afloatIndices.Count < maxAfloat) {
                    // give this one a kick based on the wind
                    afloatIndices.Add(i);

                    float windFactor = Vector3.Dot(wind, facingVector);
                    Vector3 targetVelocity = Mathf.Abs(windFactor) * wind;

                    velocity = targetVelocity;

                    // apply updraft
                    if (!isParticleInAir && windFactor > 0 && wind.magnitude > effectiveWindResistance) {
                        velocity.y += windStrength * windFactor * velocity.magnitude * facingVector.y;
                    }

                    facingVector = Vector3.RotateTowards(facingVector, velocity, 0.01f, 0);
                }
            }

            // kick up leaves when moving
            float distanceFromCenter = Vector3.Distance(gameRenderer.windSampler._WindBufferCenter, particles[i].position);
            float windRadius = 2 * gameRenderer.windSampler._DynamicWindRadius;
            float minWind = 0.2f * gameRenderer.windSampler._DynamicWindStrength * effectiveWindResistance;
            if (!isParticleInAir && distanceFromCenter < windRadius && wind.magnitude > minWind) {
                const float threshold = 0.8f;
                if (counter < threshold) {
                    float windFactor = Vector3.Dot(wind, facingVector);
                    velocity.x += windFactor * wind.magnitude * facingVector.x;
                    velocity.y += wind.magnitude * 5;
                    velocity.z += windFactor * wind.magnitude * facingVector.z;
                }
                counter = threshold + (1 - threshold) * Random.value * Mathf.SmoothStep(0, 1, distanceFromCenter / windRadius);
            }

            if (isParticleInAir) {
                // follow wind direction
                velocity = Vector3.MoveTowards(velocity, wind, windSpeed * Time.deltaTime);

                // slow down fall
                if (velocity.y < 0) {
                    velocity.y *= Random.Range(0.95f, 0.98f);
                } else {
                    velocity.y *= Random.Range(0.98f, 1.02f);
                }
            }

            particles[i].velocity = velocity;
            particleRandom[i] = new Vector4(facingVector.x, facingVector.y, facingVector.z, counter);
        }

        system.SetParticles(particles, numParticles);
        system.SetCustomParticleData(particleRandom, ParticleSystemCustomData.Custom1);
    }

    private void OnDrawGizmos()
    {
        if (!debugViewVelocity) {
            return;
        }

        for (int i = 0; i < numParticles; i++) {
            bool isParticleInAir = Mathf.Abs(particles[i].velocity.y) > 0.001f;
            if (isParticleInAir) {
                Gizmos.color = Color.green;
                Gizmos.DrawSphere(particles[i].position, 0.1f);
            }
            Gizmos.color = Color.red * particleRandom[i].w;
            Gizmos.DrawLine(particles[i].position, particles[i].position + (Vector3)(particleRandom[i]));
        }
    }
}
