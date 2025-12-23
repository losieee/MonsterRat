// 3Project_Server.js
const express = require("express");
const cors = require("cors");
const { WebSocketServer } = require("ws");
const { nanoid } = require("nanoid");

const PORT = 3000;

const app = express();
app.use(cors());
app.use(express.json());

// rooms: roomId -> state
const rooms = new Map();        // { roomId, hostKey, createdAt, playerCount, started, hostClientId }
const roomConns = new Map();    // roomId -> { clients:Set(ws) }
const clientToRoom = new Map(); // clientId -> roomId

// ---------- util ----------
function getQueryParam(url, key) {
  try {
    const u = new URL(url, "http://localhost");
    return u.searchParams.get(key);
  } catch {
    return null;
  }
}

function publicRoomList() {
  return Array.from(rooms.values())
    .map(r => ({
      roomId: r.roomId,
      createdAt: r.createdAt,
      playerCount: r.playerCount,
      started: r.started
    }))
    .sort((a, b) => b.createdAt - a.createdAt);
}

function broadcastRoomsUpdate(wss) {
  const payload = JSON.stringify({ type: "rooms_update", rooms: publicRoomList() });
  for (const c of wss.clients) {
    if (c.readyState === 1) c.send(payload);
  }
}

function broadcastRoomState(roomId) {
  const room = rooms.get(roomId);
  const conn = roomConns.get(roomId);
  if (!room || !conn) return;

  const payload = JSON.stringify({
    type: "room_state",
    roomId,
    playerCount: room.playerCount,
    canStart: room.playerCount === 2 && !room.started,
    started: room.started,
    hostClientId: room.hostClientId || ""
  });

  for (const c of conn.clients) {
    if (c.readyState === 1) c.send(payload);
  }
}

function deleteRoom(roomId) {
  rooms.delete(roomId);
  roomConns.delete(roomId);
  console.log(`[ROOM] deleted room=${roomId}`);
}

// ---------- REST ----------
app.get("/rooms", (req, res) => {
  res.json({ rooms: publicRoomList() });
});

app.post("/rooms/create", (req, res) => {
  const { clientId } = req.body;
  if (!clientId) return res.status(400).json({ ok: false, error: "NO_CLIENT_ID" });
  if (clientToRoom.has(clientId)) return res.status(409).json({ ok: false, error: "ALREADY_IN_ROOM" });

  const roomId = nanoid(6).toUpperCase();
  const hostKey = nanoid(12);

  rooms.set(roomId, {
    roomId,
    hostKey,
    createdAt: Date.now(),
    playerCount: 0,
    started: false,
    hostClientId: null
  });

  roomConns.set(roomId, { clients: new Set() });

  const host = req.headers.host; // "ip:3000"
  res.json({
    ok: true,
    roomId,
    hostKey,
    wsUrl: `ws://${host}/ws?roomId=${roomId}&clientId=${clientId}&hostKey=${hostKey}`
  });
});

app.post("/rooms/join", (req, res) => {
  const { roomId, clientId } = req.body;
  if (!clientId) return res.status(400).json({ ok: false, error: "NO_CLIENT_ID" });
  if (clientToRoom.has(clientId)) return res.status(409).json({ ok: false, error: "ALREADY_IN_ROOM" });

  if (!roomId || !rooms.has(roomId) || !roomConns.has(roomId)) {
    return res.status(404).json({ ok: false, error: "ROOM_NOT_FOUND" });
  }

  // 2명 제한(REST 단계에서도 체크)
  const conn = roomConns.get(roomId);
  if (conn.clients.size >= 2) return res.status(409).json({ ok: false, error: "ROOM_FULL" });

  const host = req.headers.host;
  res.json({
    ok: true,
    roomId,
    wsUrl: `ws://${host}/ws?roomId=${roomId}&clientId=${clientId}`
  });
});

// ---------- server + ws ----------
const server = app.listen(PORT, "0.0.0.0", () => {
  console.log(`Lobby server listening on http://0.0.0.0:${PORT}`);
});

const wss = new WebSocketServer({ server, path: "/ws" });

wss.on("connection", (ws, req) => {
  const roomId = getQueryParam(req.url, "roomId");
  const clientId = getQueryParam(req.url, "clientId");
  const hostKey = getQueryParam(req.url, "hostKey");

  if (!roomId || !clientId) {
    ws.close(1008, "NO_ROOM_OR_CLIENT");
    return;
  }
  if (!rooms.has(roomId) || !roomConns.has(roomId)) {
    ws.close(1008, "ROOM_NOT_FOUND");
    return;
  }
  if (clientToRoom.has(clientId)) {
    ws.close(1008, "ALREADY_IN_ROOM");
    return;
  }

  const room = rooms.get(roomId);
  const conn = roomConns.get(roomId);

  if (conn.clients.size >= 2) {
    ws.close(1008, "ROOM_FULL");
    return;
  }

  ws.roomId = roomId;
  ws.clientId = clientId;

  conn.clients.add(ws);
  clientToRoom.set(clientId, roomId);

  // 인원 최신화는 Set size 기준
  room.playerCount = conn.clients.size;

  // hostKey 맞으면 호스트 확정
  if (hostKey && hostKey === room.hostKey) {
    room.hostClientId = clientId;
  }
  // 호스트 없으면 첫 입장자가 호스트
  if (!room.hostClientId) {
    room.hostClientId = clientId;
  }

  ws.send(JSON.stringify({
    type: "joined",
    roomId,
    isHost: room.hostClientId === clientId,
    playerCount: room.playerCount
  }));

  broadcastRoomState(roomId);
  broadcastRoomsUpdate(wss);

  ws.on("message", (data) => {
    let msg;
    try { msg = JSON.parse(data.toString()); } catch { return; }

    if (msg.type === "start_game") {
      if (clientId !== room.hostClientId) return;
      if (room.playerCount !== 2) return;
      if (room.started) return;

      // ✅ 호스트가 보낸 hostIp가 없으면 시작하지 않게 막기
      const hostIp = (msg.hostIp || "").trim();
      if (!hostIp) {
        ws.send(JSON.stringify({ type: "error", error: "HOST_IP_EMPTY" }));
        return;
      }

      room.started = true;

      const payload = JSON.stringify({
        type: "game_start",
        roomId,
        sceneName: msg.sceneName || "GameScene",
        hostIp,
        port: 7777
      });

      for (const c of conn.clients) {
        if (c.readyState === 1) c.send(payload);
      }
    }
  });

  ws.on("close", () => {
    // 방이 이미 삭제됐으면 종료
    if (!rooms.has(roomId) || !roomConns.has(roomId)) {
      clientToRoom.delete(clientId);
      return;
    }

    const room2 = rooms.get(roomId);
    const conn2 = roomConns.get(roomId);

    conn2.clients.delete(ws);
    clientToRoom.delete(clientId);

    room2.playerCount = conn2.clients.size;
    room2.started = false;

    // 호스트가 나갔으면 남은 사람에게 승계
    if (room2.hostClientId === clientId) {
      room2.hostClientId = null;
      const first = conn2.clients.values().next().value;
      if (first) room2.hostClientId = first.clientId;
    }

    // 0명이면 방 삭제
    if (conn2.clients.size === 0) {
      deleteRoom(roomId);
      broadcastRoomsUpdate(wss);
      return;
    }

    broadcastRoomState(roomId);
    broadcastRoomsUpdate(wss);
  });
});
