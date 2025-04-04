/* 
 * Snake
 * 
 * Base game created by straker on GitHub
 *  https://gist.github.com/straker/81b59eecf70da93af396f963596dfdc5
 * 
 * Implemented by Kyle Wittman
 * 
 * Fall 2023, ETSU
 * 
 */

var canvas = document.getElementById('game');
var context = canvas.getContext('2d');

// the canvas width & height, snake x & y, and the apple x & y, all need to be a multiples of the grid size in order for collision detection to work
// (e.g. 16 * 25 = 400)
var grid = 16;
var count = 0;

//Game Over State
let isGameOver = false;

// Structure for holding data for the snake
var snake = {
  x: 160,
  y: 160,

  // snake velocity. moves one grid length every frame in either the x or y direction
  dx: grid,
  dy: 0,

  // keep track of all grids the snake body occupies
  cells: [],

  // length of the snake. grows when eating an apple
  maxCells: 4
};

// Structure for holding data for the current apple
var apple = {
  x: 320,
  y: 320
};

// get random whole numbers in a specific range
// see https://stackoverflow.com/a/1527820/2124254
function getRandomInt(min, max) {
  return Math.floor(Math.random() * (max - min)) + min;
}

function showStartScreen() {
    context.font = '36px Arial';
    context.textAlign = 'center';
    context.fillText('Press space to start', canvas.width / 2, canvas.height / 2);
}

// game loop
function loop() {

    if (isGameOver) {
        //Game Over State
        //Does Nothing right now
        //Need to change state
    }
    else {
        //Game is running state
        // Loop that now uses a base counter of 1350/15 (90 millisecond delay) to simulate 30 frames rather
        // than pulling the browsers framerate.
        setTimeout(() => {
            requestAnimationFrame(loop);
        }, 1350 / 15)

        //count = 0; // Reset the FPS counter
        context.clearRect(0, 0, canvas.width, canvas.height);

        // move snake by it's velocity
        snake.x += snake.dx;
        snake.y += snake.dy;

        // wrap snake position horizontally on edge of screen
        if (snake.x < 0) { // Left side of the screen
            snake.x = canvas.width - grid;
        }
        else if (snake.x >= canvas.width) { // Right side of the screen
            snake.x = 0;
        }

        // wrap snake position vertically on edge of screen
        if (snake.y < 0) { // Top of the screen
            snake.y = canvas.height - grid;
        }
        else if (snake.y >= canvas.height) { // Bottom of the screen
            snake.y = 0;
        }

        // keep track of where snake has been. front of the array is always the head
        snake.cells.unshift({ x: snake.x, y: snake.y });

        // remove cells as we move away from them
        if (snake.cells.length > snake.maxCells) {
            snake.cells.pop();
        }

        // draw apple
        context.fillStyle = 'red';
        context.fillRect(apple.x, apple.y, grid - 1, grid - 1);

        // draw snake one cell at a time
        context.fillStyle = 'green';
        snake.cells.forEach(function (cell, index) {

            // drawing 1 px smaller than the grid creates a grid effect in the snake body so you can see how long it is
            context.fillRect(cell.x, cell.y, grid - 1, grid - 1);

            // snake ate apple
            if (cell.x === apple.x && cell.y === apple.y) {
                snake.maxCells++;


                // Show score to website, updated after the above code runs
                const playerScore = document.getElementById("playerScore");

                // Has to be - 4 to displace the start length
                // This has to be here to change the textContent of the 'Play.cshtml' to print the correct current score.
                playerScore.textContent = `Current Score: ${snake.maxCells - 4}`;


                // canvas is 400x400 which is 25x25 grids
                apple.x = getRandomInt(0, 25) * grid;
                apple.y = getRandomInt(0, 25) * grid;
            }

            // check collision with all cells after this one (modified bubble sort)
            for (var i = index + 1; i < snake.cells.length; i++) {

                // snake occupies same space as a body part. End game
                if (cell.x === snake.cells[i].x && cell.y === snake.cells[i].y) {
                    isGameOver = true;

                    /* Reset game data. Idea maybe make a function with this 
                    snake.x = 160;
                    snake.y = 160;
                    snake.cells = [];
                    snake.maxCells = 4;
                    snake.dx = grid;
                    snake.dy = 0;
                    playerScore.textContent = `Current Score: ${0}`;
                    apple.x = getRandomInt(0, 25) * grid;
                    apple.y = getRandomInt(0, 25) * grid;
                    */
                }
            }
        });
    }
}

// listen to keyboard events to move the snake
document.addEventListener('keydown', function(e) {
  // prevent snake from backtracking on itself by checking that it's
  // not already moving on the same axis (pressing left while moving
  // left won't do anything, and pressing right while moving left
  // shouldn't let you collide with your own body)

  // left arrow key
  if (e.which === 37 && snake.dx === 0) {
    snake.dx = -grid;
    snake.dy = 0;
  }
  // up arrow key
  else if (e.which === 38 && snake.dy === 0) {
    snake.dy = -grid;
    snake.dx = 0;
  }
  // right arrow key
  else if (e.which === 39 && snake.dx === 0) {
    snake.dx = grid;
    snake.dy = 0;
  }
  // down arrow key
  else if (e.which === 40 && snake.dy === 0) {
    snake.dy = grid;
    snake.dx = 0;
  }
});

// start the game
// when the player presses the spacebar, the loop begins
document.body.onkeyup = function (e) {
    if (e.keyCode == 32)
    {
        requestAnimationFrame(loop);
    }
}

showStartScreen();