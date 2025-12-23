const express = require("express");
const cors = require("cors");
const { WebSocketServer } = require("ws");
const { nanoid } = require("nanoid");

const PORT = 3000;

const app = express();
app.use(cors());
app.use(express.json());

// -----------------------------
// 데이터 구조
// -----------------------------
// rooms: roomId -> (REST로 내려줄 "표시용" 데이터만 저장)
const rooms = new Map();

// roomConns: roomId -> { clients:Set<WebSocket>, hostWs:WebSocket|null }
const roomConns = new Map();

// clientToRoom: clientId -> roomId (이미 방에 있는지 체크)
const clientToRoom = new Map();

// -----------------------------
// 유틸
// -----------------------------
function getQueryParam(url, key) {
  try {
    const u = new URL(url, "http://localhost");
    return u.searchParams.get(key);
  } catch {
    return null;
  }
}

function getPublicRoomsList() {
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
  const payload = JSON.stringify({
    type: "rooms_update",
    rooms: getPublicRoomsList()
  });

  for (const client of wss.clients) {
    if (client.readyState === 1) client.send(payload);
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

function broadcastGameStart(roomId, sceneName) {
  const conn = roomConns.get(roomId);
  if (!conn) return;

  const payload = JSON.stringify({
    type: "game_start",
    roomId,
    sceneName
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

// -----------------------------
// REST API
// -----------------------------

// 방 목록
app.get("/rooms", (req, res) => {
  res.json({ rooms: getPublicRoomsList() });
});

// 방 생성 (clientId 필요)
app.post("/rooms/create", (req, res) => {
  const { clientId } = req.body;

  if (!clientId) {
    return res.status(400).json({ ok: false, error: "NO_CLIENT_ID" });
  }

  if (clientToRoom.has(clientId)) {
    return res.status(409).json({ ok: false, error: "ALREADY_IN_ROOM" });
  }

  const roomId = nanoid(6).toUpperCase();
  const hostKey = nanoid(12);

  rooms.set(roomId, {
    roomId,
    hostKey,           // 서버 내부용(REST 목록에는 노출 안 함)
    createdAt: Date.now(),
    playerCount: 0,
    started: false,
    hostClientId: null,
  });

  roomConns.set(roomId, {
    clients: new Set(),
    hostWs: null
  });

  // Unity가 호출한 서버 주소 그대로 wsUrl 만들어주기
  // req.headers.host => "192.168.0.10:3000" 형태
  const host = req.headers.host;

  res.json({
    ok: true,
    roomId,
    hostKey, // 호스트만 저장해야 함 (게스트에게는 보내지 마)
    wsUrl: `ws://${host}/ws?roomId=${roomId}&clientId=${clientId}&hostKey=${hostKey}`
  });
});

// 방 입장 (clientId 필요)
app.post("/rooms/join", (req, res) => {
  const { roomId, clientId } = req.body;

  if (!clientId) {
    return res.status(400).json({ ok: false, error: "NO_CLIENT_ID" });
  }

  if (clientToRoom.has(clientId)) {
    return res.status(409).json({ ok: false, error: "ALREADY_IN_ROOM" });
  }

  if (!roomId || !rooms.has(roomId) || !roomConns.has(roomId)) {
    return res.status(404).json({ ok: false, error: "ROOM_NOT_FOUND" });
  }

  // 아직 WS 연결 전이지만 2명 제한을 REST 단계에서도 한번 체크(선택)
  const conn = roomConns.get(roomId);
  if (conn.clients.size >= 2) {
    return res.status(409).json({ ok: false, error: "ROOM_FULL" });
  }

  const host = req.headers.host;

  res.json({
    ok: true,
    roomId,
    wsUrl: `ws://${host}/ws?roomId=${roomId}&clientId=${clientId}` // hostKey 없음
  });
});

// -----------------------------
// HTTP 서버 + WebSocket
// -----------------------------
const server = app.listen(PORT, "0.0.0.0", () => {
  console.log(`Lobby server listening on http://0.0.0.0:${PORT}`);
});

const wss = new WebSocketServer({ server, path: "/ws" });

wss.on("connection", (ws, req) => {
  const roomId = getQueryParam(req.url, "roomId");
  const clientId = getQueryParam(req.url, "clientId");
  const hostKey = getQueryParam(req.url, "hostKey"); // 호스트만 있음

  if (!roomId || !clientId) {
    ws.close(1008, "NO_ROOM_OR_CLIENT");
    return;
  }

  if (!rooms.has(roomId) || !roomConns.has(roomId)) {
    ws.close(1008, "ROOM_NOT_FOUND");
    return;
  }

  // 이미 다른 방에 있으면 거절
  if (clientToRoom.has(clientId)) {
    ws.close(1008, "ALREADY_IN_ROOM");
    return;
  }

  const room = rooms.get(roomId);
  const conn = roomConns.get(roomId);

  // 2명 제한
  if (conn.clients.size >= 2) {
    ws.close(1008, "ROOM_FULL");
    return;
  }

  // 등록
  ws.roomId = roomId;
  ws.clientId = clientId;
  ws.isHost = !!(hostKey && hostKey === room.hostKey);

  // 호스트는 hostWs로 저장(호스트 1명)
  if (ws.isHost) {
    conn.hostWs = ws;
  }

  conn.clients.add(ws);
  clientToRoom.set(clientId, roomId);

  // 최신 playerCount는 무조건 Set size로
  room.playerCount = conn.clients.size;

  // hostKey로 호스트 지정 (사실상 방 만든 사람)
  if (hostKey && hostKey === room.hostKey) {
    room.hostClientId = clientId;
  }

  // 호스트가 아직 없다면, 첫 입장자를 호스트로 승격
  if (!room.hostClientId) {
    room.hostClientId = clientId;
  }

  console.log(`[WS] join room=${roomId} client=${clientId} count=${room.playerCount} isHost=${ws.isHost}`);

  // joined 메시지(클라에서 "호스트만 버튼 보여주기"에 사용)
  ws.send(JSON.stringify({
    type: "joined",
    roomId,
    isHost: ws.isHost,
    playerCount: room.playerCount
  }));

  // 상태 브로드캐스트
  broadcastRoomState(roomId);
  broadcastRoomsUpdate(wss);

  // 메시지 처리
  ws.on("message", (data) => {
    const raw = data.toString();
    console.log("WS message:", raw);

    let msg;
    try { msg = JSON.parse(raw); } catch { return; }

    // 게임 시작
    if (msg.type === "start_game") {
      // 호스트만 + 2명일 때만 + 아직 시작 안함
      if (ws.clientId !== room.hostClientId) return;
      if (room.playerCount !== 2) return;
      if (room.started) return;

      room.started = true;

      // 상태 갱신 먼저
      broadcastRoomState(roomId);
      broadcastRoomsUpdate(wss);

      // 둘 다 씬 이동
      const sceneName = msg.sceneName || "GameScene";
      broadcastGameStart(roomId, sceneName);

      return;
    }
  });

  // 연결 종료 처리
  ws.on("close", () => {
    // conn/room이 이미 삭제됐을 수도 있으니 방어
    if (!rooms.has(roomId) || !roomConns.has(roomId)) {
      if (ws.clientId) clientToRoom.delete(ws.clientId);
      return;
    }

    const room2 = rooms.get(roomId);
    const conn2 = roomConns.get(roomId);

    conn2.clients.delete(ws);

    if (ws.clientId) clientToRoom.delete(ws.clientId);

    // 호스트가 나가면 단순히 hostWs 비움(남은 사람이 start 못함)
    if (conn2.hostWs === ws) conn2.hostWs = null;

    room2.playerCount = conn2.clients.size;
    room2.started = false;

    // 호스트가 나갔다면 남은 사람을 호스트로 승격
    if (room.hostClientId === ws.clientId) {
      room.hostClientId = null;

      // 남아있는 사람 중 첫 번째를 새 호스트로
      const first = conn.clients.values().next().value; // Set의 첫 요소
      if (first) {
        room.hostClientId = first.clientId;
      }
    }

    // 0명이면 방 삭제
    if (conn2.clients.size === 0) {
      deleteRoom(roomId);
      broadcastRoomsUpdate(wss);
      return;
    }

    // 남아있으면 상태 갱신
    broadcastRoomState(roomId);
    broadcastRoomsUpdate(wss);
  });
});
