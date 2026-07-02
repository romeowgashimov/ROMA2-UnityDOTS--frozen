using Unity.Entities;
using Unity.NetCode;

namespace ROMA2.Logic.Data
{
    public enum CharacterState : byte
    {
        None   = 0,
        Idle   = 1 << 0,
        Move   = 1 << 1,
        Run    = 1 << 2,
        Attack = 1 << 3,
    }

    [GhostComponent]
    public struct GhostCharacterState : IComponentData
    {
        [GhostField] public CharacterState Value;
    }
}