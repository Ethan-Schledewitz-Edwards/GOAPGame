namespace ArchieAndrews.PrefabBrush
{
    public enum PB_ActiveTab { About, PrefabPaint, Settings, PrefabErase }
    public enum PB_Direction { Up, Down, Left, Right, Forward, Backward }
    public enum PB_EraseDetectionType { Collision, Distance }
    public enum PB_EraseTypes { PrefabsInBrush, PrefabsInBounds }
    public enum PB_PaintType { Surface, Physics, Single }
    public enum PB_ParentingStyle {Surface, SingleTempParent, ClosestTempFromList, TempRoundRobin }
    public enum PB_PrefabDisplayType { Icon, List }
    public enum PB_SaveApplicationType { Set, Multiply }
    public enum PB_ScaleType {SingleValue, MultiAxis }
    public enum PB_DragModType { Position, Rotation, Scale }
    public enum PB_FilterCheckType {CheckPrefab, CheckSurface}
}