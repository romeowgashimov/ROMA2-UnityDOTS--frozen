using ROMA2.Logic.Client.Data;
using Unity.Entities;
using UnityEngine;

namespace ROMA2.Logic.Client.Bakers
{
    public class UIAuthoring : MonoBehaviour
    {
        public float DistanceOffset = 1.5f;
        public float SizeOffset = 1.0f;
        public float HeightOffset = 0.6f;
        
        private class UIBaker : Baker<UIAuthoring>
        {
            public override void Bake(UIAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<UpdatedHP4UI>(entity);
                AddComponent<UpdatedMana4UI>(entity);
                AddComponent<UpdatedChars>(entity);
                AddComponent<PortraitProperties>(entity, new()
                {
                    DistanceOffset = authoring.DistanceOffset,
                    SizeOffset = authoring.SizeOffset,
                    HeightOffset = authoring.HeightOffset,
                });
                // Нужен для отобржанеия урона
                AddBuffer<CachedDamageElement>(entity);
            }
        }
    }
}