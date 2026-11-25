const view = document.querySelector("#view");
const map = document.querySelector("#deck-map");
const exploration = document.querySelector("#exploration");
const plot = document.querySelector(".plot");
const canvas = document.querySelector("#schematic");
const ctx = canvas.getContext("2d");
const routeButton = document.querySelector("#route-toggle");
const systems = document.querySelector("#systems");
const closeButton = document.querySelector("#close-map");
const openButton = document.querySelector("#open-map");
const preview = new URLSearchParams(location.search);
const places = {
  gate: { name: "Power Gate", status: "Unpowered", symbol: "door", copy: "Restore power to the gate to continue to Life Support." },
  objective: { name: "Life Support", status: "Unsurveyed", symbol: "unsurveyed", copy: "The scrubber is beyond the Power Gate. Your route ends at the gate until it has power." }
};

function layout() {
  const scale = Math.min(innerWidth / 1920, innerHeight / 1080);
  view.style.left = (innerWidth - 1920 * scale) / 2 + "px";
  view.style.top = (innerHeight - 1080 * scale) / 2 + "px";
  view.style.transform = `scale(${scale})`;
}

function setMap(open, moveFocus = true) {
  map.hidden = !open;
  exploration.hidden = open;
  if (moveFocus) (open ? closeButton : openButton).focus();
}

function selectPlace(key) {
  const place = places[key];
  document.querySelector("#selection-name").textContent = place.name;
  document.querySelector("#selection-status").textContent = place.status;
  document.querySelector("#selection-symbol").className = "symbol " + place.symbol;
  document.querySelector(".condition").toggleAttribute("data-unconfirmed", key !== "gate");
  document.querySelector("#selection-copy").textContent = place.copy;
  for (const marker of document.querySelectorAll(".marker"))
    marker.setAttribute("aria-pressed", String(marker.dataset.place === key));
}

function line(points, color, width = 1) {
  ctx.beginPath();
  points.forEach(([x, y], index) => index === 0 ? ctx.moveTo(x, y) : ctx.lineTo(x, y));
  ctx.strokeStyle = color;
  ctx.lineWidth = width;
  ctx.stroke();
}

function area(points, fill, stroke, dashed = false) {
  ctx.beginPath();
  points.forEach(([x, y], index) => index === 0 ? ctx.moveTo(x, y) : ctx.lineTo(x, y));
  ctx.closePath();
  ctx.fillStyle = fill;
  ctx.fill();
  ctx.strokeStyle = stroke;
  ctx.lineWidth = 2;
  ctx.setLineDash(dashed ? [8, 6] : []);
  ctx.stroke();
  ctx.setLineDash([]);
}

function draw() {
  ctx.clearRect(0, 0, canvas.width, canvas.height);
  for (let x = 18; x < canvas.width; x += 30) line([[x + .5, 0], [x + .5, 708]], "#20323b");
  for (let y = 18; y < canvas.height; y += 30) line([[0, y + .5], [1288, y + .5]], "#20323b");
  const visited = "#23353e";
  const wall = "#758b93";
  const survey = "#182932";
  const unknown = "#526970";

  // Scaled schematic adapted from the Service Deck concept.
  area([[105,110],[124,91],[468,91],[487,110],[487,410],[420,410],[420,439],[312,439],[312,410],[105,410]], visited, wall);
  area([[510,217],[528,199],[746,199],[765,217],[765,415],[691,415],[691,439],[614,439],[614,415],[510,415]], visited, wall);
  area([[833,125],[852,106],[1155,106],[1175,125],[1175,439],[1035,439],[1035,414],[944,414],[944,439],[833,439]], survey, unknown, true);
  area([[818,439],[1175,439],[1175,555],[1044,555],[1044,567],[954,567],[954,555],[818,555]], survey, unknown, true);
  area([[860,568],[954,568],[954,557],[1044,557],[1044,568],[1175,568],[1175,663],[1157,681],[860,681]], survey, unknown, true);
  area([[105,439],[312,439],[312,414],[420,414],[420,439],[614,439],[614,415],[691,415],[691,439],[813,439],[813,555],[105,555]], visited, wall);
  for (const [x, y] of [[159,235],[247,235],[335,235],[159,314],[247,314],[335,314]]) {
    ctx.fillStyle = "#2a424d";
    ctx.fillRect(x, y, 42, 67);
    ctx.strokeStyle = "#536b74";
    ctx.lineWidth = 1;
    ctx.strokeRect(x + .5, y + .5, 41, 66);
    line([[x + 6, y + 8],[x + 36, y + 8]], "#758b93");
  }
  for (const x of [548,595,642,689]) {
    ctx.strokeStyle = "#536b74";
    ctx.strokeRect(x + .5, 316.5, 23, 41);
  }
  line([[324,437],[409,437]], "#526970", 2);
  line([[625,437],[680,437]], "#526970", 2);
  line([[817,439],[817,555]], "#f19b92", 4);
  if (routeButton.getAttribute("aria-pressed") === "true") {
    line([[483,501],[483,525],[789,525]], "#f1ba68", 3);
    line([[780,518],[790,525],[780,532]], "#f1ba68", 3);
  }
}

openButton.addEventListener("click", () => setMap(true));
closeButton.addEventListener("click", () => setMap(false));
for (const button of document.querySelectorAll("[data-place]"))
  button.addEventListener("click", () => selectPlace(button.dataset.place));
routeButton.addEventListener("click", () => {
  const shown = routeButton.getAttribute("aria-pressed") === "true";
  routeButton.setAttribute("aria-pressed", String(!shown));
  routeButton.querySelector("span").textContent = shown ? "Show route" : "Hide route";
  draw();
});
systems.addEventListener("change", () => {
  plot.dataset.systems = String(systems.checked);
  if (!systems.checked) selectPlace("gate");
});
document.addEventListener("keydown", event => {
  if (event.repeat || event.metaKey || event.ctrlKey || event.altKey) return;
  if (event.key.toLowerCase() === "m" || (event.key === "Escape" && !map.hidden)) {
    event.preventDefault();
    setMap(map.hidden);
  }
});
addEventListener("resize", layout);
layout();
draw();
if (preview.get("selection") === "objective") selectPlace("objective");
if (preview.has("explore")) setMap(false, false);
