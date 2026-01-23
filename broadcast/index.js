import express from "express";
import fs from "fs";
import path from "path";

const app = express();
const PORT = 3000;

const servers = [];
const __dirname = path.resolve();

const leaderboardPath = path.join(__dirname, "leaderboard.json");

console.log("Leaderboard path:", leaderboardPath);

app.use(express.json());
app.use(express.urlencoded({ extended: true }));


app.post("/create", (req, res) => {
  const serverInfo = req.body;
  const newServer = {
    name: serverInfo.name,
    ip: serverInfo.ip,
    port: serverInfo.port,
    password: serverInfo.password || "",
    players: serverInfo.players || 0
  }
  servers.push(newServer);
  res.status(201).send("Server added");
});

app.post("/server/:port", (req, res) => {
  const port = Number(req.params.port);
  const newAmountPlayers = req.body.players;
  console.log(req.body);
  console.log(port);
  const serverIndex = servers.findIndex(server => server.port == port);
  if (serverIndex !== -1) {
    servers[serverIndex].players = newAmountPlayers;
    res.status(200).send("Player count updated");
  } else {
    res.status(404).send("Server not found");
  }
  if (serverIndex !== -1) {
    console.log(port, servers[serverIndex].players);
  } else {
    console.log(port, "server not found");
  }
});

app.get("/servers", (req, res) => {
  res.status(200).json({games: servers});
});

app.delete("/server/:port", (req, res) => {
  const port = Number(req.params.port);
  const serverIndex = servers.findIndex(server => server.port == port);
  if (serverIndex !== -1) {
    servers.splice(serverIndex, 1);
    res.status(200).send("Server deleted");
  } else {
    res.status(404).send("Server not found");
  }
});

app.get("/server/:port", (req, res) => {
  const port = Number(req.params.port);
  const server = servers.find(server => server.port == port);
  if (server) {
    res.status(200).json({ password: server.password });
  } else {
    res.status(404).send("Server not found");
  }
});

app.get("/leaderboard", (req, res) => {
  fs.readFile(leaderboardPath, "utf8", (err, data) => {
    if (err) {
      console.error("Error reading leaderboard file:", err);
      res.status(500).send("Internal Server Error");
      return;
    }

    const leaderboardData = JSON.parse(data);
    res.status(200).json({ entries: leaderboardData });
  });
});

app.post("/leaderboard", (req, res) => {
  const name = req.body.playerName;
  const score = req.body.score;
  const date = new Date().toISOString();

  fs.readFile(leaderboardPath, "utf8", (err, data) => {
    if (err) {
      console.error("Error reading leaderboard file:", err);
      res.status(500).send("Internal Server Error");
      return;
    }

    const leaderboardData = JSON.parse(data);
    const newEntry = { playerName: name, score: score, date: date };
    console.log("New leaderboard entry:", newEntry);
    leaderboardData.push(newEntry);
    leaderboardData.sort((a, b) => b.score - a.score);
    fs.writeFile(leaderboardPath, JSON.stringify(leaderboardData, null, 2), (err) => {
      if (err) {
        console.error("Error writing to leaderboard file:", err);
        res.status(500).send("Internal Server Error");
        return;
      }
      res.status(201).send("Leaderboard entry added");
    });
  });
});

app.listen(PORT, () => {
  console.log(`Server is running on http://localhost:${PORT}`);
});