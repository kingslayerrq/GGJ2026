public enum PlayerMovementMode
{
    Free,
    VerticalOnly
}

public static class PlayerEnterState
{
    public static PlayerMovementMode movementMode = PlayerMovementMode.Free;
}