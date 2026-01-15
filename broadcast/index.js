import express from "express";

const app = express();
const PORT = 3000;

const servers = [{ name: "Abc", ip: "127.0.0.1", port: 8080, password: "secret" }];

app.use(express.json());
app.use(express.urlencoded({ extended: true }));

app.put("/", (req, res) => {
  const serverInfo = req.body;
  servers.push(serverInfo);
  res.status(201).send("Server added");
});

app.get("/", (req, res) => {
  res.status(200).json({servers});
});

app.listen(PORT, () => {
  console.log(`Server is running on http://localhost:${PORT}`);
});