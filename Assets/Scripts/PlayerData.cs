using System;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public struct PlayerData : IEquatable<PlayerData>, INetworkSerializable 
{
    public ulong clientId;
    public FixedString64Bytes playerName;
    public FixedString64Bytes playerId;
    public FixedString4096Bytes playerImageBase64;


    public bool Equals(PlayerData other) {
        return
            clientId == other.clientId &&
            playerName == other.playerName &&
            playerId == other.playerId &&
            playerImageBase64 == other.playerImageBase64;
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter {
        serializer.SerializeValue(ref clientId);
        serializer.SerializeValue(ref playerName);
        serializer.SerializeValue(ref playerId);
        serializer.SerializeValue(ref playerImageBase64);
    }
}
