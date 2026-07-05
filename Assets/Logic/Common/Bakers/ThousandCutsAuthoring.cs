using ROMA2.Logic.Common.Databases;
using ROMA2.Logic.Navigation;
using Unity.Entities;
using UnityEngine;

namespace ROMA2.Logic.Common.Abilities
{
    public class ThousandCutsAuthoring : MonoBehaviour
    {
        public int MagicalDamage;
        public int MagicalPercentage;
        public int CutCount;
        public int MaxCutCount;
        public float MaxCutDelay;

         private class ThousandCutsBaker : Baker<ThousandCutsAuthoring>
        {
            public override void Bake(ThousandCutsAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.None);
                AddComponent<AbilityCommand>(entity);
                AddComponent<ThousandCutsAbility>(entity, new()
                {
                    MagicalDamage = authoring.MagicalDamage,
                    MagicalPercentage = authoring.MagicalPercentage,
                    CutCount = authoring.CutCount,
                    MaxCutCount = authoring.MaxCutCount,
                    CutDelay = authoring.MaxCutDelay,
                    MaxCutDelay = authoring.MaxCutDelay,
                    IsFirstUse = true
                });
                AddComponent<IgnoreRegistrationInGrid>(entity);
            }
        }
    }
}