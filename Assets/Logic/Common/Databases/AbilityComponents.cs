using Unity.Entities;

namespace ROMA2.Logic.Common.Databases
{
    public struct DeathShotAbility : IComponentData
    {
        public int PhysicalPercentage;
    }

    public struct DeathSphereAbility : IComponentData
    {
        public int MagicalPercentage;
    }

    public struct ThousandCutsAbility : IComponentData, IEnableableComponent
    {
        public int MagicalPercentage;
        public int MagicalDamage;
        public int CutCount;
        public int MaxCutCount;
        public float CutDelay;
        public float MaxCutDelay;
        public bool IsFirstUse;
        public int AbilityIndex;
        public bool NeedToConfirmAbilities;
    }
}