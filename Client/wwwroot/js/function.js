window.getDivContent = function (elementId) {
  if (document.title == "Arrangement") {
    var div = document.getElementById(elementId);
    return div.innerHTML;
  }
};

var listners = [];

var M = [];

window.Init = function () {
  shipOrientation = {
    LeftRight: 0,
    TopDown: 1,
  };

  if (document.title == "Arrangement") {
    if (listners.length) {
      listners.forEach((l) => {
        l[2].removeEventListener(l[0], l[1]);
      });

      listners = [];
    }

    let startGame = false;
    let isHandlerPlacement = false;

    const getElement = (id) => {
      console.log(id);
      return document.getElementById(id);
    };

    const getCoordinates = (el) => {
      if (el) {
        const coords = el.getBoundingClientRect();

        return {
          left: coords.left + window.pageXOffset,
          right: coords.right + window.pageXOffset,
          top: coords.top + window.pageYOffset,
          bottom: coords.bottom + window.pageYOffset,
        };
      }
    };

    let field = document.getElementById("field");

    window.onbeforeunload = function (e) {
      field = document.getElementById("field");
    };

    class Field {
      static FIELD_SIDE = 330;
      static SHIP_SIDE = 33;

      static SHIP_DATA = {
        fourdeck: [1, 4],
        tripledeck: [2, 3],
        doubledeck: [3, 2],
        singledeck: [4, 1],
      };

      constructor(field) {
        this.field = field;
        this.squadron = {};
        this.matrix = [];

        let { left, right, top, bottom } = getCoordinates(this.field);

        this.fieldLeft = left;
        this.fieldRight = right;
        this.fieldTop = top;
        this.fieldBottom = bottom;
      }

      static createMatrix() {
        return [...Array(10)].map(() => Array(10).fill(0));
      }

      static getRandom = (n) => Math.floor(Math.random() * (n + 1));

      cleanField() {
        while (this.field.firstChild) {
          this.field.removeChild(this.field.firstChild);
        }
        this.squadron = {};
        this.matrix = Field.createMatrix();
      }

      randomLocationShips() {
        window.getPageRef("Arrangement").invokeMethodAsync("ClearShips");

        for (let type in Field.SHIP_DATA) {
          let count = Field.SHIP_DATA[type][0];
          let decks = Field.SHIP_DATA[type][1];

          for (let i = 0; i < count; i++) {
            let options = this.getCoordsDecks(decks);
            options.decks = decks;
            options.shipname = type + String(i + 1);
            const ship = new Ships(this, options);

            ship.createShip();

            window
              .getPageRef("Arrangement")
              .invokeMethodAsync(
                "AddShip",
                options.y,
                options.x,
                options.decks,
                options.kx == 0 ? shipOrientation.LeftRight : shipOrientation.TopDown,
              );
          }
        }

        M = Array.from(human.matrix.flat());
        console.log(JSON.stringify(human.matrix));
      }

      getCoordsDecks(decks) {
        let kx = Field.getRandom(1),
          ky = kx == 0 ? 1 : 0,
          x,
          y;

        if (kx == 0) {
          x = Field.getRandom(9);
          y = Field.getRandom(10 - decks);
        } else {
          x = Field.getRandom(10 - decks);
          y = Field.getRandom(9);
        }

        const obj = { x, y, kx, ky };
        const result = this.checkLocationShip(obj, decks);
        if (!result) return this.getCoordsDecks(decks);
        return obj;
      }

      checkLocationShip(obj, decks) {
        let { x, y, kx, ky, fromX, toX, fromY, toY } = obj;

        fromX = x == 0 ? x : x - 1;

        if (x + kx * decks == 10 && kx == 1) toX = x + kx * decks;
        else if (x + kx * decks < 10 && kx == 1) toX = x + kx * decks + 1;
        else if (x == 9 && kx == 0) toX = x + 1;
        else if (x < 9 && kx == 0) toX = x + 2;

        fromY = y == 0 ? y : y - 1;
        if (y + ky * decks == 10 && ky == 1) toY = y + ky * decks;
        else if (y + ky * decks < 10 && ky == 1) toY = y + ky * decks + 1;
        else if (y == 9 && ky == 0) toY = y + 1;
        else if (y < 9 && ky == 0) toY = y + 2;

        if (toX === undefined || toY === undefined) return false;

        if (
          this.matrix.slice(fromX, toX).filter((arr) => arr.slice(fromY, toY).includes(1)).length >
          0
        )
          return false;
        return true;
      }
    }

    class Ships {
      constructor(self, { x, y, kx, ky, decks, shipname }) {
        this.player = self === human ? human : computer;
        this.field = self.field;
        this.shipname = shipname;
        this.decks = decks;
        this.x = x;
        this.y = y;
        this.kx = kx;
        this.ky = ky;
        this.hits = 0;
        this.arrDecks = [];
      }

      static showShip(self, shipname, x, y, kx) {
        const div = document.createElement("div");
        const classname = shipname.slice(0, -1);
        const dir = kx == 1 ? " vertical" : "";

        div.setAttribute("id", shipname);
        div.className = `ship ${classname}${dir}`;
        div.style.cssText = `left:${y * Field.SHIP_SIDE}px; top:${x * Field.SHIP_SIDE}px;`;
        self.field.appendChild(div);
      }

      createShip() {
        let { player, field, shipname, decks, x, y, kx, ky, hits, arrDecks, k = 0 } = this;

        while (k < decks) {
          let i = x + k * kx,
            j = y + k * ky;
          player.matrix[i][j] = 1;
          arrDecks.push([i, j]);
          k++;
        }

        player.squadron[shipname] = { arrDecks, hits, x, y, kx, ky };
        if (player === human) {
          Ships.showShip(human, shipname, x, y, kx);
          if (Object.keys(player.squadron).length == 10) {
            buttonPlay.hidden = false;
          }
        }

        M = Array.from(human.matrix.flat());
      }
    }

    class Placement {
      static FRAME_COORDS = getCoordinates(field);

      constructor() {
        this.dragObject = {};
        this.pressed = false;
      }

      static getShipName = (el) => el.getAttribute("id");
      static getCloneDecks = (el) => {
        const type = Placement.getShipName(el).slice(0, -1);
        return Field.SHIP_DATA[type][1];
      };

      setObserver() {
        if (isHandlerPlacement) {
          return;
        }

        M = Array.from(human.matrix.flat());

        window.addEventListener(
          "resize",
          function (event) {
            Placement.FRAME_COORDS = getCoordinates(field);
          },
          true,
        );

        let onMouseDownBind = this.onMouseDown.bind(this);
        document.addEventListener("mousedown", onMouseDownBind);
        listners.push(["mousedown", onMouseDownBind, document]);

        let onMouseMoveBind = this.onMouseMove.bind(this);
        document.addEventListener("mousemove", onMouseMoveBind);
        listners.push(["mousemove", onMouseDownBind, document]);

        let onMouseUpBind = this.onMouseUp.bind(this);
        document.addEventListener("mouseup", onMouseUpBind);
        listners.push(["mouseup", onMouseUpBind, document]);

        let rotationBind = this.rotationShip.bind(this);
        field.addEventListener("contextmenu", rotationBind);
        listners.push(["contextmenu", rotationBind, field]);

        isHandlerPlacement = true;
      }

      onMouseDown(e) {
        if (e.which != 1 || startGame) return;

        const el = e.target.closest(".ship");

        if (!el) {
          return;
        }

        this.pressed = true;

        this.dragObject = {
          el,
          parent: el.parentElement,
          next: el.nextElementSibling,
          downX: e.pageX,
          downY: e.pageY,
          left: el.offsetLeft,
          top: el.offsetTop,
          kx: 0,
          ky: 1,
        };

        if (el.parentElement === field) {
          const name = Placement.getShipName(el);

          this.dragObject.kx = human.squadron[name].kx;
          this.dragObject.ky = human.squadron[name].ky;
        }
      }

      onMouseMove(e) {
        if (!this.pressed || !this.dragObject.el) return;

        let { left, right, top, bottom } = getCoordinates(this.dragObject.el);

        if (!this.clone) {
          this.decks = Placement.getCloneDecks(this.dragObject.el);
          this.clone = this.creatClone({ left, right, top, bottom }) || null;

          if (!this.clone) {
            return;
          }

          this.shiftX = this.dragObject.downX - left;
          this.shiftY = this.dragObject.downY - top;

          this.clone.style.zIndex = "1000";

          document.body.appendChild(this.clone);

          this.removeShipFromSquadron(this.clone);
        }

        let currentLeft = Math.round(e.pageX - this.shiftX),
          currentTop = Math.round(e.pageY - this.shiftY);

        this.clone.style.left = `${currentLeft}px`;
        this.clone.style.top = `${currentTop}px`;

        if (
          left >= Placement.FRAME_COORDS.left - 14 &&
          right <= Placement.FRAME_COORDS.right + 14 &&
          top >= Placement.FRAME_COORDS.top - 14 &&
          bottom <= Placement.FRAME_COORDS.bottom + 14
        ) {
          this.clone.classList.remove("unsuccess");
          this.clone.classList.add("success");

          const { x, y } = this.getCoordsCloneInMatrix({ left, right, top, bottom });
          const obj = {
            x,
            y,
            kx: this.dragObject.kx,
            ky: this.dragObject.ky,
          };

          const result = human.checkLocationShip(obj, this.decks);
          if (!result) {
            this.clone.classList.remove("success");
            this.clone.classList.add("unsuccess");
          }
        } else {
          this.clone.classList.remove("success");
          this.clone.classList.add("unsuccess");
        }

        M = Array.from(human.matrix.flat());
      }

      onMouseUp(e) {
        this.pressed = false;

        if (!this.clone) {
          return;
        }

        if (this.clone.classList.contains("unsuccess")) {
          this.clone.classList.remove("unsuccess");
          this.clone.rollback();
        } else {
          this.createShipAfterMoving();
        }

        this.removeClone();
        M = Array.from(human.matrix.flat());
      }

      rotationShip(e) {
        e.preventDefault();

        if (e.which != 3 || startGame) return;

        const el = e.target.closest(".ship");
        const name = Placement.getShipName(el);

        if (human.squadron[name].decks == 1) {
          return;
        }

        const obj = {
          kx: human.squadron[name].kx == 0 ? 1 : 0,
          ky: human.squadron[name].ky == 0 ? 1 : 0,

          x: human.squadron[name].x,
          y: human.squadron[name].y,
        };

        const decks = human.squadron[name].arrDecks.length;
        this.removeShipFromSquadron(el);
        human.field.removeChild(el);

        const result = human.checkLocationShip(obj, decks);

        if (!result) {
          obj.kx = obj.kx == 0 ? 1 : 0;
          obj.ky = obj.ky == 0 ? 1 : 0;
        }

        obj.shipname = name;
        obj.decks = decks;

        const ship = new Ships(human, obj);
        ship.createShip();

        if (!result) {
          const el = getElement(name);
          el.classList.add("unsuccess");
          setTimeout(() => {
            el.classList.remove("unsuccess");
          }, 750);
        }

        window
          .getPageRef("Arrangement")
          .invokeMethodAsync(
            "AddShip",
            obj.y,
            obj.x,
            obj.decks,
            obj.kx == 0 ? shipOrientation.LeftRight : shipOrientation.TopDown,
          );

        M = Array.from(human.matrix.flat());
        console.log(JSON.stringify(human.matrix));
      }

      creatClone() {
        const clone = this.dragObject.el;
        const oldPosition = this.dragObject;

        clone.rollback = () => {
          if (oldPosition.parent == field) {
            clone.style.left = `${oldPosition.left}px`;
            clone.style.top = `${oldPosition.top}px`;
            clone.style.zIndex = "";
            oldPosition.parent.insertBefore(clone, oldPosition.next);
            this.createShipAfterMoving();
          } else {
            clone.removeAttribute("style");
            oldPosition.parent.insertBefore(clone, oldPosition.next);
          }
        };

        M = Array.from(human.matrix.flat());

        return clone;
      }

      removeClone() {
        delete this.clone;
        this.dragObject = {};
      }

      createShipAfterMoving() {
        const coords = getCoordinates(this.clone);

        let { left, top, x, y } = this.getCoordsCloneInMatrix(coords);

        this.clone.style.left = `${left}px`;
        this.clone.style.top = `${top}px`;

        field.appendChild(this.clone);
        this.clone.classList.remove("success");

        const options = {
          shipname: Placement.getShipName(this.clone),
          x,
          y,

          kx: this.dragObject.kx,
          ky: this.dragObject.ky,

          decks: this.decks,
        };

        window
          .getPageRef("Arrangement")
          .invokeMethodAsync(
            "AddShip",
            options.y,
            options.x,
            options.decks,
            options.kx == 0 ? shipOrientation.LeftRight : shipOrientation.TopDown,
          );

        const ship = new Ships(human, options);
        ship.createShip();
        field.removeChild(this.clone);

        M = Array.from(human.matrix.flat());
        console.log(JSON.stringify(human.matrix));
      }

      getCoordsCloneInMatrix({ left, right, top, bottom } = coords) {
        let computedLeft = left - Placement.FRAME_COORDS.left,
          computedRight = right - Placement.FRAME_COORDS.left,
          computedTop = top - Placement.FRAME_COORDS.top,
          computedBottom = bottom - Placement.FRAME_COORDS.top;

        const obj = {};

        console.log(JSON.stringify({ left, right, top, bottom }));

        let o = this.dragObject.kx == 0 ? shipOrientation.LeftRight : shipOrientation.TopDown;

        let ft =
          computedTop < 0
            ? 0
            : computedBottom > Field.FIELD_SIDE
            ? Field.FIELD_SIDE - Field.SHIP_SIDE * (o == shipOrientation.TopDown ? this.decks : 1)
            : computedTop;

        let fl =
          computedLeft < 0
            ? 0
            : computedRight > Field.FIELD_SIDE
            ? Field.FIELD_SIDE - Field.SHIP_SIDE * (o == shipOrientation.LeftRight ? this.decks : 1)
            : computedLeft;

        obj.top = Math.round(ft / Field.SHIP_SIDE) * Field.SHIP_SIDE;
        obj.left = Math.round(fl / Field.SHIP_SIDE) * Field.SHIP_SIDE;

        obj.x = obj.top / Field.SHIP_SIDE;
        obj.y = obj.left / Field.SHIP_SIDE;

        console.log(JSON.stringify(obj));

        M = Array.from(human.matrix.flat());

        return obj;
      }

      removeShipFromSquadron(el) {
        const name = Placement.getShipName(el);

        if (!human.squadron[name]) {
          return;
        }

        const arr = human.squadron[name].arrDecks;
        for (let coords of arr) {
          const [x, y] = coords;
          human.matrix[x][y] = 0;
        }

        let x = arr[0][0];
        let y = arr[0][1];

        window.getPageRef("Arrangement").invokeMethodAsync("RemoveShip", y, x);

        delete human.squadron[name];

        M = Array.from(human.matrix.flat());
        console.log(JSON.stringify(human.matrix));
      }
    }
    const shipsCollection = getElement("ships_collection");
    const initialShips = document.querySelector(".wrap + .initial-ships");
    const buttonPlay = getElement("play");
    const human = new Field(field);

    getElement("type_placement").addEventListener("click", function (e) {
      if (e.target.tagName != "SPAN") {
        return;
      }

      buttonPlay.hidden = true;
      human.cleanField();

      let initialShipsClone = "";
      const type = e.target.dataset.target;

      const typeGeneration = {
        random() {
          shipsCollection.hidden = true;
          human.randomLocationShips();

          M = Array.from(human.matrix.flat());
        },
        manually() {
          window.getPageRef("Arrangement").invokeMethodAsync("ClearShips");

          if (shipsCollection.children.length > 1) {
            shipsCollection.removeChild(shipsCollection.lastChild);
          }
          initialShipsClone = initialShips.cloneNode(true);
          shipsCollection.appendChild(initialShipsClone);
          initialShipsClone.hidden = false;
          shipsCollection.hidden = false;

          M = Array.from(human.matrix.flat());
        },
      };
      typeGeneration[type]();

      const placement = new Placement();
      placement.setObserver();

      M = Array.from(human.matrix.flat());
    });

    window.getMatrix = () => M;
  }
};

var listners2 = [];

var matrix = [];
var res = "";

window.GameInit = function (matrixText) {
  if (document.title == "Play game") {
    if (listners2.length) {
      listners2.forEach((l) => {
        l[2].removeEventListener(l[0], l[1]);
      });

      listners2 = [];
    }

    matrix = JSON.parse(matrixText);

    const SHIP_SIDE = 33;

    const getCoordinates = (el) => {
      if (el === null) return { left: 0, right: 0, top: 0, bottom: 0 };

      const coords = el.getBoundingClientRect();

      return {
        left: coords.left + window.pageXOffset,
        right: coords.right + window.pageXOffset,
        top: coords.top + window.pageYOffset,
        bottom: coords.bottom + window.pageYOffset,
      };
    };

    const myfield = document.getElementById("my_field");
    let otherfield = document.getElementById("other_field");
    res = "";

    let { left, right, top, bottom } = getCoordinates(otherfield);

    let resizeHandler = function (event) {
      if (document.title == "Play game") {
        otherfield = document.getElementById("other_field");

        ({ left, right, top, bottom } = getCoordinates(otherfield));
      }
    };

    window.addEventListener("resize", resizeHandler, true);
    listners2.push(["resize", resizeHandler, window]);

    function transformCoordsInMatrix(e) {
      const x = Math.trunc((e.pageY - top) / SHIP_SIDE);
      const y = Math.trunc((e.pageX - left) / SHIP_SIDE);
      return [x, y];
    }

    function showIcons(field, [x, y], iconClass) {
      const span = document.createElement("span");

      span.className = `icon-field ${iconClass}`;
      span.style.cssText = `left:${y * SHIP_SIDE}px; top:${x * SHIP_SIDE}px;`;
      field.appendChild(span);

      return span.outerHTML;
    }

    function miss(x, y) {
      matrix[x][y] = 3;
      return showIcons(otherfield, [x, y], "dot");
    }
    function shot(x, y) {
      matrix[x][y] = 4;
      return showIcons(otherfield, [x, y], "red-cross");
    }

    window.getmiss = function (x, y) {
      return showIcons(myfield, [x, y], "dot");
    };
    window.getshot = function (x, y) {
      return showIcons(myfield, [x, y], "red-cross");
    };

    window.onOtherField = function (e) {
      let [x, y] = transformCoordsInMatrix(e);

      if (matrix[x][y] == 0) {
        res = `${x} ${y} false`;
        return miss(x, y);
      } else if (matrix[x][y] == 1) {
        let alive = matrix.flat().reduce((total, x) => total + (x == 1), 0);

        if (alive == 1) {
          res = "Victory";
        } else {
          res = `${x} ${y} true`;
        }
        return shot(x, y);
      } else {
        res = "Skip";
      }
    };

    window.getResults = function () {
      return res;
    };
  }
};
