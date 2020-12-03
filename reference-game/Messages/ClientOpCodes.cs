namespace MultiplayerHost.ReferenceGame.Messages
{
    /// <summary>
    /// Defines operations for Client 2 Server messages.
    /// </summary>
    public enum ClientOpCode
    {
        InvalidLower = 0,

        /// <summary>
        /// Adds a player to the game. 
        /// Note: a full blown game would have an authentication mechanism outside the game logic scope.
        /// After registration/confirmation the user would be added to the user list.
        /// </summary>
        EnterGame,

        /// <summary>
        /// Client requests player state data.
        /// Payload: []
        /// </summary>
        Login,

        /// <summary>
        /// Client requests to enter sector.
        /// After entering a sector the client receives a <see cref="ServerOpCode.SectorSync"/> 
        /// message followed by <see cref="ServerOpCode.SectorPositionSync"/> messages.
        /// Payload: [sectorId]
        /// </summary>
        EnterSector,

        /// <summary>
        /// Client requests to navigate towards destination.
        /// Payload: [sectorId:shipId:state:x:y]
        /// Note: state can be Moving or Returning
        /// </summary>
        Move,

        /// <summary>
        /// Client requests to attack the target.
        /// If target is not in range a Move message will be auto-generated.
        /// Payload: [sectorId:shipId:target id:targetType]
        /// Note: targetType can be Mob or Ship
        /// </summary>
        Attack,

        /// <summary>
        /// Building build request. 
        /// Can be used on free tiles for initial building or on tiles 
        /// containing the same building for upgrading the building level.
        /// Payload: [templateId:level:x:y]
        /// </summary>
        Build,

        /// <summary>
        /// Move building request.
        /// Payload: [buildingId:newX:newY]
        /// </summary>
        MoveBuilding,

        /// <summary>
        /// Starts building production.
        /// Payload: [buildingId:itemid:count]
        /// </summary>
        Produce,

        /// <summary>
        /// Starts resource collection.
        /// Payload: [itemId:x:y]
        /// </summary>
        Collect,

        /// <summary>
        /// Player switches between land and sea.
        /// Payload: [worldview enum]
        /// </summary>
        SetWorldView,

        /// <summary>
        /// Possible only when in sea view.
        /// </summary>
        MapMove,

        /// <summary>
        /// Placeholder for unsupported codes. 
        /// Logs error and returns.
        /// </summary>
        Unsupported,

        InvalidUpper
    }
}
