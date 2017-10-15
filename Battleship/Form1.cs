/***********************************************************

Progammer:  Kyle Koschak
            Nick Sattler 
            Martin Morales 
            Brian Kelley

Purpose: Multiplayer battleship game designed to be played on separate computers.
         Transfers data between clients via database.
************************************************************/
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Data.SqlClient;
using System.Timers;

namespace assign5
{

    public partial class Form1 : Form
    {
        private static System.Timers.Timer poll, pollSetup, pollTurn;
        player clientPlayer = new player();
        player opponent = new player();
        public string conString = "server=;pwd=;database=;"; // get database connection string

/****************************************************************
Form1_Paint

Use: Event handler for whenever a repaint is called.

Parameters: The sender and PaintEventArgs e.

Returns: nothing
****************************************************************/
        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            using(Graphics g = e.Graphics){
                Pen line = new Pen(Color.Black);
               //Draw top and bottom grids
                for(int x = 180; x <= 580; x+=40){
                    g.DrawLine(line, x, 40, x, 440);
                    g.DrawLine(line, x, 475, x, 875);
                    for (int y = 40; y <= 440; y+=40)
                    {
                        g.DrawLine(line, 180, y, 580, y);
                        g.DrawLine(line, 180, y+435, 580, y+435);
                    }
                }
                drawBoard();
            }
        }

        /****************************************************************
        Form1_FormClosing

        Use: Event for when the game is closed. Necessary to remove player and board from database.

        Parameters: The sender and FormEventArgs e.

        Returns: nothing
        ****************************************************************/
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            e.Cancel = true;

            string removePlayerCommand = "DELETE FROM Player WHERE PlayerID = " + Convert.ToString(clientPlayer.PlayerID) + ";";
            string removeBoardCommand = "DELETE FROM Board WHERE PlayerID = " + Convert.ToString(clientPlayer.PlayerID) + ";";
            using (SqlConnection exitConnection = new SqlConnection(conString))
            {
                exitConnection.Open();
                //Remove board and player from database on exit
                try
                {
                    SqlCommand removePlayer = new SqlCommand(removeBoardCommand, exitConnection);
                    removePlayer.ExecuteNonQuery();
                    removePlayer = new SqlCommand(removePlayerCommand, exitConnection);
                    removePlayer.ExecuteNonQuery();
                }
                catch (SqlException exitEx)
                {
                    MessageBox.Show(exitEx.Message);
                }
            }
            e.Cancel = false;
        }

        /****************************************************************
        Form1_Load

        Use: Sets up the game on form load.

        Parameters: The sender and EventArgs e.

        Returns: nothing
        ****************************************************************/
        private void Form1_Load(object sender, EventArgs e)
        {
            //On form load set gamestate for clientPlayer to 0 and create "empty" 2D array to store ship locations later for both clientPlayer and opponent
            clientPlayer.gameState = 0;
            clientPlayer.board = new int[10, 10] { { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }, { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }, { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }, { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }, { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }, { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }, { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }, { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }, { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }, { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 } };
            opponent.board = new int[10, 10] { { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }, { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }, { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }, { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }, { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }, { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }, { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }, { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }, { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }, { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 } };
            string createPlayer = "INSERT INTO Player (PlayerState) VALUES (0);";
            string fetchPlayerID = "SELECT SCOPE_IDENTITY()";
            if (numPlayers() < 2)
            {
                using (SqlConnection MyConnection = new SqlConnection(conString))
                {
                    MyConnection.Open();

                    //Create player in database
                    try
                    {
                        SqlCommand makePlayer = new SqlCommand(createPlayer, MyConnection);
                        makePlayer.ExecuteNonQuery();
                        makePlayer = new SqlCommand(fetchPlayerID, MyConnection);
                        clientPlayer.PlayerID = Convert.ToInt32(makePlayer.ExecuteScalar());
                    }
                    catch (SqlException ex)
                    {
                        MessageBox.Show(ex.Message);
                    }

                    //Check for player 2
                    rotateButton.Visible = false;
                    if (numPlayers() > 1)
                    {
                        addOpponent();
                        setBoard();
                    }
                    //If player 2 not initially found, create a poll to check for once a second player has connected.
                    else
                    {
                        poll = new System.Timers.Timer(1000);
                        poll.Elapsed += pollSecondPlayer;
                        poll.AutoReset = true;
                        poll.Enabled = true;
                    }
                }
            }
            else
            {
                MessageBox.Show("There are already 2 players playing. \n Try again later.");
                Application.Exit();
            }
        }

/****************************************************************
rotateButton_Click

Use: Event for button click to change orientation of ship placement.

Parameters: The sender and EventArgs e.

Returns: nothing
****************************************************************/
        private void rotateButton_Click(object sender, EventArgs e)
        {
            if (clientPlayer.orientation == true)
            {
                clientPlayer.orientation = false;
            }
            else
            {
                clientPlayer.orientation = true;
            }
        }

        /****************************************************************
        exitButton_Click

        Use: Exits the game on button click

        Parameters: The sender and EventArgs e.

        Returns: nothing
        ****************************************************************/
        private void exitButton_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

/****************************************************************
Form1_Load

Use: Calls various functions depending on what phase the game is in.

Parameters: The sender and MouseEventArgs e.

Returns: nothing
****************************************************************/
        private void Form1_MouseUp(object sender, MouseEventArgs e)
        {
            //If in clientPlayer turn phase, allow firing on location and then pass turn to opponent
            if(clientPlayer.gameState == 8){
                clientTurnActions(e.X, e.Y);
            }

            //If in setting board phases, go through placing each ship
            else if (clientPlayer.gameState == 1 || clientPlayer.gameState == 2 || clientPlayer.gameState == 3 || clientPlayer.gameState == 4 || clientPlayer.gameState == 5)
            {
                placeShips(e.X, e.Y);
            }
        }

        /****************************************************************
        Form1_MouseMove

        Use: Calls functions to draw board cursor indicators.

        Parameters: The sender and MouseEventArgs e.

        Returns: nothing
        ****************************************************************/
        private void Form1_MouseMove(object sender, MouseEventArgs e)
        {
            //If in client player turn, draw single square on top grid
            if (clientPlayer.gameState == 8)
            {
                int x = (((e.X + 20) / 40) * 40) - 20;
                int y = (((e.Y) / 40) * 40);
                if (x >= 180 && x < 580 && y >= 40 && y < 440)
                {
                    drawCurrentSquare(x, y);
                }
            }

            //If in board setting phases, draw the current ship to be placed
            if (clientPlayer.gameState == 1 || clientPlayer.gameState == 2 || clientPlayer.gameState == 3 || clientPlayer.gameState == 4 || clientPlayer.gameState == 5)
            {
                int x = (((e.X + 20) / 40) * 40) - 20;
                int y = (((e.Y) / 40) * 40) - 5;

                if (x >= 180 && x < 580 && y >= 475 && y < 875)
                {
                    clientPlayer.bottomMouseColumn = ((e.X - 140) / 40)-1;
                    clientPlayer.bottomMouseRow = ((e.Y - 440) / 40)-1;

                    if (clientPlayer.gameState == 1)
                    {
                        drawFiveShip(x, y);
                    }
                    else if (clientPlayer.gameState == 2)
                    {
                        drawFourShip(x,y);
                    }
                    else if (clientPlayer.gameState == 3 || clientPlayer.gameState == 4)
                    {
                        drawThreeShip(x, y);
                    }
                    else if (clientPlayer.gameState == 5)
                    {
                        drawTwoShip(x, y);
                    }

                }
            }
        }

        /****************************************************************
        Form1

        Use: Form1 constructor subscribes to some events

        Parameters: None

        Returns: nothing
        ****************************************************************/
        public Form1()
        {
            InitializeComponent();
            //Subscribe to paint, mousemove, and mouseup events
            this.FormClosing += this.Form1_FormClosing;
            this.Paint += this.Form1_Paint;
            this.MouseMove += new MouseEventHandler(this.Form1_MouseMove);
            this.MouseUp += new MouseEventHandler(this.Form1_MouseUp);
        }

        /****************************************************************
        pollSecondPlayer

        Use: Periodically check if a second player has connected then add opponent ID and go to setBoard

        Parameters: Object source, ElapsedEventArgs e

        Returns: nothing
        ****************************************************************/
        private void pollSecondPlayer(Object source, ElapsedEventArgs e)
        {
            //Poll for waiting for second player to connect        
            if (numPlayers() > 1)
            {
                poll.Enabled = false;
                addOpponent();
                setBoard();
            }
        }

        /****************************************************************
        setBoard

        Use: Changes text and game state for other functions to pass a check on

        Parameters: None

        Returns: nothing
        ****************************************************************/
        private void setBoard()
        {
            clientPlayer.gameState = 1;
            clientPlayer.orientation = true;
            waitLabel.Text = "Player Found.\nSet your board.";
            rotateButton.Visible = true;
        }

        /****************************************************************
        drawFiveShip

        Use: Draws a ship indicator of length 5

        Parameters: int x, int y (x,y coordinates for top left of rectangle)

        Returns: nothing
        ****************************************************************/
        private void drawFiveShip(int x, int y)
        {
            Rectangle currentSquare;
            SolidBrush b;
            if (clientPlayer.orientation == true)
            {
                currentSquare = new Rectangle(x, y, 200, 40);
            }
            else
            {
                currentSquare = new Rectangle(x, y, 40, 200);
            }

            if (x + 160 >= 580 && clientPlayer.orientation == true)
            {
                b = new SolidBrush(Color.Red);
            }
            else if (y + 160 >= 875 && clientPlayer.orientation == false)
            {
                b = new SolidBrush(Color.Red);
            }
            else
            {
                b = new SolidBrush(Color.Yellow);
            }
            using (Graphics g = this.CreateGraphics())
            {
                g.Clear(Form1.DefaultBackColor);
                g.FillRectangle(b, currentSquare);
                drawBoard();
            }
        }

        /****************************************************************
        drawFourShip

        Use: Draws a ship indicator of length 4

        Parameters: int x, int y (x,y coordinates for top left of rectangle)

        Returns: nothing
        ****************************************************************/
        private void drawFourShip(int x, int y)
        {
            Rectangle currentSquare;
            SolidBrush b;
            if (clientPlayer.orientation == true)
            {
                currentSquare = new Rectangle(x, y, 160, 40);
            }
            else
            {
                currentSquare = new Rectangle(x, y, 40, 160);
            }

            if (x + 120 >= 580 && clientPlayer.orientation == true)
            {
                b = new SolidBrush(Color.Red);
            }
            else if (y + 120 >= 875 && clientPlayer.orientation == false)
            {
                b = new SolidBrush(Color.Red);
            }
            else
            {
                b = new SolidBrush(Color.Yellow);
            }
            using (Graphics g = this.CreateGraphics())
            {
                g.Clear(Form1.DefaultBackColor);
                g.FillRectangle(b, currentSquare);
                drawBoard();
            }
        }

        /****************************************************************
        drawThreeShip

        Use: Draws a ship indicator of length 3

        Parameters: int x, int y (x,y coordinates for top left of rectangle)

        Returns: nothing
        ****************************************************************/
        private void drawThreeShip(int x, int y)
        {
            Rectangle currentSquare;
            SolidBrush b;
            if (clientPlayer.orientation == true)
            {
                currentSquare = new Rectangle(x, y, 120, 40);
            }
            else
            {
                currentSquare = new Rectangle(x, y, 40, 120);
            }

            if (x + 80 >= 580 && clientPlayer.orientation == true)
            {
                b = new SolidBrush(Color.Red);
            }
            else if (y + 80 >= 875 && clientPlayer.orientation == false)
            {
                b = new SolidBrush(Color.Red);
            }
            else
            {
                b = new SolidBrush(Color.Yellow);
            }
            using (Graphics g = this.CreateGraphics())
            {
                g.Clear(Form1.DefaultBackColor);
                g.FillRectangle(b, currentSquare);
                drawBoard();
            }
        }

        /****************************************************************
        drawTwoShip

        Use: Draws a ship indicator of length 2

        Parameters: int x, int y (x,y coordinates for top left of rectangle)

        Returns: nothing
        ****************************************************************/
        private void drawTwoShip(int x, int y)
        {
            Rectangle currentSquare;
            SolidBrush b;
            if (clientPlayer.orientation == true)
            {
                currentSquare = new Rectangle(x, y, 80, 40);
            }
            else
            {
                currentSquare = new Rectangle(x, y, 40, 80);
            }

            if (x + 40 >= 580 && clientPlayer.orientation == true)
            {
                b = new SolidBrush(Color.Red);
            }
            else if (y + 40 >= 875 && clientPlayer.orientation == false)
            {
                b = new SolidBrush(Color.Red);
            }
            else
            {
                b = new SolidBrush(Color.Yellow);
            }
            using (Graphics g = this.CreateGraphics())
            {
                g.Clear(Form1.DefaultBackColor);
                g.FillRectangle(b, currentSquare);
                drawBoard();
            }
        }

        /****************************************************************
        drawCurrentSquare

        Use: Draws a single tile indactor

        Parameters: int x, int y (x,y coordinates for top left of rectangle)

        Returns: nothing
        ****************************************************************/
        private void drawCurrentSquare(int x, int y)
        {
            Rectangle currentSquare = new Rectangle(x,y,40,40);
            SolidBrush b = new SolidBrush(Color.Yellow);
            Graphics g = this.CreateGraphics();
            g.Clear(Form1.DefaultBackColor);
            g.FillRectangle(b, currentSquare);
            drawBoard();
        }

        /****************************************************************
        drawBoard

        Use: Draws the board grid and every element within it (ships, hits, misses)
         * Draws the elements, not the indicators.

        Parameters: none

        Returns: nothing
        ****************************************************************/
        private void drawBoard()
        {
            using (Graphics g = this.CreateGraphics())
            {
                Pen line = new Pen(Color.Black);
                SolidBrush b = new SolidBrush(Color.DarkGray);
                SolidBrush h = new SolidBrush(Color.Orange);
                SolidBrush m = new SolidBrush(Color.White);
                Rectangle ship;
                Rectangle hit;
                Rectangle miss;

                for (int x = 180; x <= 580; x += 40)
                {
                    g.DrawLine(line, x, 40, x, 440);
                    g.DrawLine(line, x, 475, x, 875);
                    for (int y = 40; y <= 440; y += 40)
                    {
                        g.DrawLine(line, 180, y, 580, y);
                        g.DrawLine(line, 180, y + 435, 580, y + 435);
                    }

                }
                //Draw bottom board pieces
                int e = 180, f = 475;
                for (int i = 0; i < 10; i++)
                {
                    for (int q = 0; q < 10; q++)
                    {
                        if (clientPlayer.board[q, i] != 0 && clientPlayer.board[q,i] != 6 && clientPlayer.board[q,i] != 7)
                        {
                            ship = new Rectangle(e, f, 40, 40);
                            g.FillRectangle(b, ship);
                        }
                        else if(clientPlayer.board[q,i] == 6){
                            miss = new Rectangle(e, f, 40, 40);
                            g.FillRectangle(m, miss);
                        }
                        else if (clientPlayer.board[q, i] == 7)
                        {
                            hit = new Rectangle(e, f, 40, 40);
                            g.FillRectangle(h, hit);
                        }
                        e += 40;
                    }
                    f += 40;
                    e = 180;
                }

                //Draw top board pieces
                e = 180;
                f = 40;
                for (int i = 0; i < 10; i++)
                {
                    for (int q = 0; q < 10; q++)
                    {
                        if (opponent.board[q, i] == 6)
                        {
                            miss = new Rectangle(e, f, 40, 40);
                            g.FillRectangle(m, miss);
                        }
                        else if (opponent.board[q, i] == 7)
                        {
                            hit = new Rectangle(e, f, 40, 40);
                            g.FillRectangle(h, hit);
                        }
                        e += 40;
                    }
                    f += 40;
                    e = 180;
                }
            }
        }

       /****************************************************************
       tradePlayerInfo

       Use: Uploads board info to the database

       Parameters: None

       Returns: nothing
       ****************************************************************/
        private void tradePlayerInfo()
        {
            clientPlayer.gameState = 7;
            string boardSet = "UPDATE Player SET PlayerState = 7 WHERE PlayerID = " + Convert.ToString(clientPlayer.PlayerID) + ";";
            string one1="", one2="", one3="", one4="", one5="", two1="", two2="", two3="", two4="", three1="", three2="", three3="", four1="", four2="", four3="", five1="", five2="";
            int counter1 = 1, counter2 = 1, counter3 = 1, counter4 = 1, counter5 = 1;

            //Loop through 2D array and create strings to store in database
            for (int i = 0; i < 10; i++)
            {
                for (int q = 0; q < 10; q++)
                {
                    if(clientPlayer.board[i,q] == 5){
                        if (counter1 == 1) { one1 = Convert.ToString(i) + Convert.ToString(q); }
                        if (counter1 == 2) { one2 = Convert.ToString(i) + Convert.ToString(q); }
                        if (counter1 == 3) { one3 = Convert.ToString(i) + Convert.ToString(q); }
                        if (counter1 == 4) { one4 = Convert.ToString(i) + Convert.ToString(q); }
                        if (counter1 == 5) { one5 = Convert.ToString(i) + Convert.ToString(q); }
                        counter1++;
                    }
                    else if (clientPlayer.board[i, q] == 4)
                    {
                        if (counter2 == 1) { two1 = Convert.ToString(i) + Convert.ToString(q); }
                        if (counter2 == 2) { two2 = Convert.ToString(i) + Convert.ToString(q); }
                        if (counter2 == 3) { two3 = Convert.ToString(i) + Convert.ToString(q); }
                        if (counter2 == 4) { two4 = Convert.ToString(i) + Convert.ToString(q); }
                        counter2++;
                    }
                    else if (clientPlayer.board[i, q] == 3)
                    {
                        if (counter3 == 1) { three1 = Convert.ToString(i) + Convert.ToString(q); }
                        if (counter3 == 2) { three2 = Convert.ToString(i) + Convert.ToString(q); }
                        if (counter3 == 3) { three3 = Convert.ToString(i) + Convert.ToString(q); }
                        counter3++;
                    }
                    else if (clientPlayer.board[i, q] == 2)
                    {

                        if (counter4 == 1) { four1 = Convert.ToString(i) + Convert.ToString(q); }
                        if (counter4 == 2) { four2 = Convert.ToString(i) + Convert.ToString(q); }
                        if (counter4 == 3) { four3 = Convert.ToString(i) + Convert.ToString(q); }
                        counter4++;
                    }
                    else if (clientPlayer.board[i, q] == 1)
                    {
                        if (counter5 == 1) { five1 = Convert.ToString(i) + Convert.ToString(q); }
                        if (counter5 == 2) { five2 = Convert.ToString(i) + Convert.ToString(q); }
                        counter5++;
                    }
                }
            }
            string storeBoard = "INSERT INTO Board (PlayerID, Hits, one1, one2, one3, one4, one5, two1, two2, two3, two4, three1, three2, three3, four1, four2, four3, five1, five2) VALUES (" + Convert.ToString(clientPlayer.PlayerID) + ", 0," + one1 + "," + one2 + "," + one3 + "," + one4 + "," + one5 + "," 
            + two1 + "," + two2 + "," + two3 + "," + two4 + "," + three1 + "," + three2 + "," + three3 + "," + four1 + "," + four2 + "," + four3 + "," + five1 + "," + five2 + ");";
            //Store board in the database and update player state to board set
            try{
                using(SqlConnection store = new SqlConnection(conString)){
                    store.Open();
                    SqlCommand insertBoard = new SqlCommand(storeBoard, store);
                    insertBoard.ExecuteNonQuery();
                    insertBoard = new SqlCommand(boardSet, store);
                    insertBoard.ExecuteNonQuery();
                }
            }
            catch(SqlException ex){
                MessageBox.Show(ex.Message);
            }
            pollSetup = new System.Timers.Timer(1000);
            pollSetup.Elapsed += pollPostSetup;
            pollSetup.AutoReset = true;
            pollSetup.Enabled = true;
        }

        /****************************************************************
        pollPostSetup

        Use: Periodically polls the database until other player is done setting their board.
         * Once they're done fetch board info to store locally.

        Parameters: object sender, ElapsedEventArgs e

        Returns: nothing
        ****************************************************************/
        private void pollPostSetup(object sender, ElapsedEventArgs e)
        {
            string checkOpponentGameState = "SELECT PlayerState FROM Player WHERE PlayerID = " + Convert.ToString(opponent.PlayerID) + ";";
            try
            {
                using (SqlConnection conn = new SqlConnection(conString))
                {
                    conn.Open();
                    SqlCommand fetchOpponentState = new SqlCommand(checkOpponentGameState, conn);
                    opponent.gameState = Convert.ToInt32(fetchOpponentState.ExecuteScalar());
                }
            }
            catch (SqlException ex)
            {
                MessageBox.Show(ex.Message);
            }
            //If done setting the board
            if (opponent.gameState == 7)
            {
                pollSetup.Enabled = false;
                pollTurn = new System.Timers.Timer(1000);
                pollTurn.Elapsed += pollTurnMethod;
                pollTurn.AutoReset = true;

                //String to fetch where each ship tile is located from the database
                string opponentBoardQuery = "SELECT one1, one2, one3, one4, one5, two1, two2, two3, two4, three1, three2, three3, four1, four2, four3, five1, five2 FROM Board WHERE PlayerID =" + Convert.ToString(opponent.PlayerID) + ";";

                try{
                using(SqlConnection conn = new SqlConnection(conString)){

                    conn.Open();

                    SqlCommand fetchOpponentBoard = new SqlCommand(opponentBoardQuery, conn);
                    try
                    {
                        SqlDataReader myReader = fetchOpponentBoard.ExecuteReader();
                        myReader.Read();
                        IDataRecord myRecord = (IDataRecord)myReader;

                        //Split string for each ship to get coordinates (indexes in 2D array) to create client-side opponent board, starting with length 5 ship
                        for (int a = 0; a < 5; a++)
                        {
                            int row, column;
                            //Turns first character in the string from the database into a string
                            string x, y;
                            //If/else to fix the case where leading 0's are removed from the string
                            if (myRecord[a].ToString().Length == 1)
                            {
                                column = 0;
                                y = Convert.ToString(myRecord[a].ToString()[0]);
                            }
                            else
                            {
                                y = Convert.ToString(myRecord[a].ToString()[1]);
                                x = Convert.ToString(myRecord[a].ToString()[0]);
                                //Convert x into an integer to use as 2D array index
                                column = Convert.ToInt32(x);
                            }
                            //Convert y into an integer to use as 2D array index
                            row = Convert.ToInt32(y);

                            opponent.board[column, row] = 5;
                        }
                        //Four ship
                        for (int a = 5; a < 9; a++)
                        {
                            int row, column;
                            string x, y;
                            if (myRecord[a].ToString().Length == 1)
                            {
                                column = 0;
                                y = Convert.ToString(myRecord[a].ToString()[0]);
                            }
                            else
                            {
                                y = Convert.ToString(myRecord[a].ToString()[1]);
                                x = Convert.ToString(myRecord[a].ToString()[0]);
                                column = Convert.ToInt32(x);
                            }
                            row = Convert.ToInt32(y);
                            opponent.board[column, row] = 4;
                        }
                        //First three ship
                        for (int a = 9; a < 12; a++)
                        {
                            int row, column;
                            string x, y;
                            if (myRecord[a].ToString().Length == 1)
                            {
                                column = 0;
                                y = Convert.ToString(myRecord[a].ToString()[0]);
                            }
                            else
                            {
                                y = Convert.ToString(myRecord[a].ToString()[1]);
                                x = Convert.ToString(myRecord[a].ToString()[0]);
                                column = Convert.ToInt32(x);
                            }
                            row = Convert.ToInt32(y);
                            opponent.board[column, row] = 3;
                        }

                        //Second three ship
                        for (int a = 12; a < 15; a++)
                        {
                            int row, column;
                            string x, y;
                            if (myRecord[a].ToString().Length == 1)
                            {
                                column = 0;
                                y = Convert.ToString(myRecord[a].ToString()[0]);
                            }
                            else
                            {
                                y = Convert.ToString(myRecord[a].ToString()[1]);
                                x = Convert.ToString(myRecord[a].ToString()[0]);
                                column = Convert.ToInt32(x);
                            }
                            row = Convert.ToInt32(y);
                            opponent.board[column, row] = 2;
                        }

                        //Two ship
                        for (int a = 15; a < 17; a++)
                        {
                            int row, column;
                            string x, y;
                            if (myRecord[a].ToString().Length == 1)
                            {
                                column = 0;
                                y = Convert.ToString(myRecord[a].ToString()[0]);
                            }
                            else
                            {
                                y = Convert.ToString(myRecord[a].ToString()[1]);
                                x = Convert.ToString(myRecord[a].ToString()[0]);
                                column = Convert.ToInt32(x);
                            }
                            row = Convert.ToInt32(y);
                            opponent.board[column, row] = 1;
                        }



                        myReader.Close();
                    }
                    catch (Exception x)
                    {
                        MessageBox.Show(x.Message);
                    }

                }
                }catch(SqlException ex){
                    MessageBox.Show(ex.Message);
                }

                if (clientPlayer.PlayerID < opponent.PlayerID) { clientTurn(); }
                else { opponentTurn(); }
            }
        }

        /****************************************************************
        fire

        Use: Fires on opponent's board at passed in column, row

        Parameters: int x, int y (column, row of clicked tile)

        Returns: nothing
        ****************************************************************/
        private void fire(int x, int y){
            if(opponent.board[x,y] == 5){
                opponent.board[x, y] = 7;
                opponent.fiveCount++;
                opponent.hits++;
                if (opponent.fiveCount == 5) { MessageBox.Show("Battleship Sunk!"); }
            }
            else if (opponent.board[x, y] == 4)
            {
                opponent.board[x, y] = 7;
                opponent.fourCount++;
                opponent.hits++;
                if (opponent.fourCount == 4) { MessageBox.Show("Battleship Sunk!"); }
            }
            else if (opponent.board[x, y] == 3)
            {
                opponent.board[x, y] = 7;
                opponent.threeCount++;
                opponent.hits++;
                if (opponent.threeCount == 3) { MessageBox.Show("Battleship Sunk!"); }
            }
            else if (opponent.board[x, y] == 2)
            {
                opponent.board[x, y] = 7;
                opponent.twoCount++;
                opponent.hits++;
                if (opponent.twoCount == 3) { MessageBox.Show("Battleship Sunk!"); }
            }
            else if (opponent.board[x, y] == 1)
            {
                opponent.board[x, y] = 7;
                opponent.oneCount++;
                opponent.hits++;
                if (opponent.oneCount == 2) { MessageBox.Show("Battleship Sunk!"); }
            }
            else
            {
                opponent.board[x,y] = 6;
            }

            //Send target tile to database for opponent to register your moves on their bottom grid
            string target = Convert.ToString(x) + Convert.ToString(y);
            string targetCommand = "UPDATE Player SET target = " + target + "WHERE PlayerID = " + Convert.ToString(clientPlayer.PlayerID) + ";";
            try
            {
                using (SqlConnection conn = new SqlConnection(conString))
                {
                    conn.Open();
                    SqlCommand uploadTarget = new SqlCommand(targetCommand, conn);
                    uploadTarget.ExecuteNonQuery();
                }
            }
            catch (SqlException ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

       /****************************************************************
       firedOn

       Use: Fetches spot opponent last fired on, then updates clientPlayer board
        * to reflect the fired on location.

       Parameters: None

       Returns: nothing
       ****************************************************************/
        private void firedOn()
        {
            string opponentTargetCommand = "SELECT target FROM Player WHERE PlayerID = " + Convert.ToString(opponent.PlayerID) + ";";
            int row, column;

            try
            {
                using (SqlConnection conn = new SqlConnection(conString))
                {
                    conn.Open();
                    SqlCommand fetchOpponentTarget = new SqlCommand(opponentTargetCommand, conn);
                    string target = Convert.ToString(fetchOpponentTarget.ExecuteScalar());
                    string x;
                    if (target.Length == 1)
                    {
                        x = Convert.ToString(target[0].ToString()[0]);
                        column = 0;
                    }
                    else
                    {
                        column = Convert.ToInt32(target[0].ToString());
                        x = Convert.ToString(target[1].ToString()[0]);
                    }
                    row = Convert.ToInt32(x);

                    //Change to hit or miss on client board depending if ship is there or not
                    if(clientPlayer.board[column,row] == 0){ clientPlayer.board[column, row] = 6; }
                    else if (clientPlayer.board[column, row] != 0) {
                        clientPlayer.hits++;
                        clientPlayer.board[column, row] = 7;
                    }
                    drawBoard();
                    //If all your ships are sunk, lose.
                    if (clientPlayer.hits == 17)
                    {
                        MessageBox.Show("You Lost!");
                        Application.Exit();
                    }
                }
            }
            catch (SqlException ex)
            {
                MessageBox.Show(ex.Message); 
            }

        }

        /****************************************************************
        clientTurn

        Use: Actions to take when game phase is on the client's turn.

        Parameters: None

        Returns: nothing
        ****************************************************************/
        private void clientTurn()
        {
            pollTurn.Enabled = false;
            clientPlayer.gameState = 8;
            opponent.gameState = 9;
            waitLabel.Text = "Your turn.";
            firedOn();
        }

       /****************************************************************
       opponentTurn

       Use: Actions to take when it is not the client's turn (poll database until it is)

       Parameters: None

       Returns: nothing
       ****************************************************************/
        private void opponentTurn()
        {
            pollTurn.Enabled = true;
            clientPlayer.gameState = 9;
            opponent.gameState = 8;
            waitLabel.Text = "Opponent's Turn...";
        }

       /****************************************************************
       pollTurnMethod

       Use: Periodically check if opponent has finished their turn.

       Parameters: object sender, ElapsedEventArgs e

       Returns: nothing
       ****************************************************************/
        private void pollTurnMethod(object sender, ElapsedEventArgs e)
        {
            string checkOpponentGameState = "SELECT PlayerState FROM Player WHERE PlayerID = " + Convert.ToString(opponent.PlayerID) + ";";
            using (SqlConnection conn = new SqlConnection(conString))
            {
                conn.Open();
                SqlCommand fetchOpponentState = new SqlCommand(checkOpponentGameState, conn);
                opponent.gameState = Convert.ToInt32(fetchOpponentState.ExecuteScalar());
            }
            if (opponent.gameState == 9)
            {
                clientTurn();
            }
        }

       /****************************************************************
       clientTurnActions

       Use: MouseUp actions to take if at clientTurn

       Parameters: int x, int y (e.X, e.Y or location of mouse on MouseUp)

       Returns: nothing
       ****************************************************************/
        private void clientTurnActions(int x, int y)
        {
            if (x >= 180 && x < 580 && y >= 40 && y < 440)
            {
                clientPlayer.topMouseColumn = ((x - 140) / 40) - 1;
                clientPlayer.topMouseRow = ((y) / 40) - 1;

                //If selected tile is not already a hit or miss
                if (opponent.board[clientPlayer.topMouseColumn, clientPlayer.topMouseRow] != 6 && opponent.board[clientPlayer.topMouseColumn, clientPlayer.topMouseRow] != 7)
                {
                    fire(clientPlayer.topMouseColumn, clientPlayer.topMouseRow);
                    //Swap player states in database and go to opponent turn phase
                    string endTurnCommand = "UPDATE Player SET PlayerState = 9 WHERE PlayerID = " + Convert.ToString(clientPlayer.PlayerID) + "; UPDATE Player SET PlayerState = 8 WHERE PlayerID = " + Convert.ToString(opponent.PlayerID) + ";";
                    try
                    {
                        using (SqlConnection conn = new SqlConnection(conString))
                        {
                            conn.Open();
                            SqlCommand endTurn = new SqlCommand(endTurnCommand, conn);
                            clientPlayer.gameState = 9;
                            endTurn.ExecuteNonQuery();
                        }
                    }
                    catch (SqlException ex)
                    {
                        MessageBox.Show(ex.Message);
                    }
                    drawBoard();
                    //If all of opponents ships are sunk, win.
                    if (opponent.hits == 17)
                    {
                        MessageBox.Show("You Win!");
                        Application.Exit();
                    }
                    opponentTurn();
                }
            }
        }

       /****************************************************************
       placeShips

       Use: Action to take at MouseUp if at ship placeing phase

       Parameters: int a, int b (e.X, e.Y or location of mouse at MouseUp)

       Returns: nothing
       ****************************************************************/
        private void placeShips(int a, int b)
        {
            int x = (((a + 20) / 40) * 40) - 20;
            int y = (((b) / 40) * 40) - 5;
            if (x >= 180 && x < 580 && y >= 475 && y < 875)
            {

                if (clientPlayer.gameState == 1)
                {
                    if (clientPlayer.orientation == true && clientPlayer.bottomMouseColumn < 6)
                    {
                        clientPlayer.board[clientPlayer.bottomMouseColumn, clientPlayer.bottomMouseRow] = 5;
                        clientPlayer.board[clientPlayer.bottomMouseColumn + 1, clientPlayer.bottomMouseRow] = 5;
                        clientPlayer.board[clientPlayer.bottomMouseColumn + 2, clientPlayer.bottomMouseRow] = 5;
                        clientPlayer.board[clientPlayer.bottomMouseColumn + 3, clientPlayer.bottomMouseRow] = 5;
                        clientPlayer.board[clientPlayer.bottomMouseColumn + 4, clientPlayer.bottomMouseRow] = 5;
                        clientPlayer.gameState = 2;
                    }
                    else if (clientPlayer.orientation == false && clientPlayer.bottomMouseRow < 6)
                    {
                        clientPlayer.board[clientPlayer.bottomMouseColumn, clientPlayer.bottomMouseRow] = 5;
                        clientPlayer.board[clientPlayer.bottomMouseColumn, clientPlayer.bottomMouseRow + 1] = 5;
                        clientPlayer.board[clientPlayer.bottomMouseColumn, clientPlayer.bottomMouseRow + 2] = 5;
                        clientPlayer.board[clientPlayer.bottomMouseColumn, clientPlayer.bottomMouseRow + 3] = 5;
                        clientPlayer.board[clientPlayer.bottomMouseColumn, clientPlayer.bottomMouseRow + 4] = 5;
                        clientPlayer.gameState = 2;
                    }
                }
                else if (clientPlayer.gameState == 2)
                {
                    if (clientPlayer.orientation == true && clientPlayer.bottomMouseColumn < 7 && clientPlayer.board[clientPlayer.bottomMouseColumn, clientPlayer.bottomMouseRow] == 0
                        && clientPlayer.board[clientPlayer.bottomMouseColumn + 1, clientPlayer.bottomMouseRow] == 0 && clientPlayer.board[clientPlayer.bottomMouseColumn + 2, clientPlayer.bottomMouseRow] == 0
                        && clientPlayer.board[clientPlayer.bottomMouseColumn + 3, clientPlayer.bottomMouseRow] == 0)
                    {
                        clientPlayer.board[clientPlayer.bottomMouseColumn, clientPlayer.bottomMouseRow] = 4;
                        clientPlayer.board[clientPlayer.bottomMouseColumn + 1, clientPlayer.bottomMouseRow] = 4;
                        clientPlayer.board[clientPlayer.bottomMouseColumn + 2, clientPlayer.bottomMouseRow] = 4;
                        clientPlayer.board[clientPlayer.bottomMouseColumn + 3, clientPlayer.bottomMouseRow] = 4;
                        clientPlayer.gameState = 3;
                    }
                    else if (clientPlayer.orientation == false && clientPlayer.bottomMouseRow < 7 && clientPlayer.board[clientPlayer.bottomMouseColumn, clientPlayer.bottomMouseRow] == 0
                        && clientPlayer.board[clientPlayer.bottomMouseColumn, clientPlayer.bottomMouseRow + 1] == 0 && clientPlayer.board[clientPlayer.bottomMouseColumn, clientPlayer.bottomMouseRow + 2] == 0
                        && clientPlayer.board[clientPlayer.bottomMouseColumn, clientPlayer.bottomMouseRow + 3] == 0)
                    {
                        clientPlayer.board[clientPlayer.bottomMouseColumn, clientPlayer.bottomMouseRow] = 4;
                        clientPlayer.board[clientPlayer.bottomMouseColumn, clientPlayer.bottomMouseRow + 1] = 4;
                        clientPlayer.board[clientPlayer.bottomMouseColumn, clientPlayer.bottomMouseRow + 2] = 4;
                        clientPlayer.board[clientPlayer.bottomMouseColumn, clientPlayer.bottomMouseRow + 3] = 4;
                        clientPlayer.gameState = 3;
                    }
                }
                else if (clientPlayer.gameState == 3)
                {
                    if (clientPlayer.orientation == true && clientPlayer.bottomMouseColumn < 8 && clientPlayer.board[clientPlayer.bottomMouseColumn, clientPlayer.bottomMouseRow] == 0
                        && clientPlayer.board[clientPlayer.bottomMouseColumn + 1, clientPlayer.bottomMouseRow] == 0 && clientPlayer.board[clientPlayer.bottomMouseColumn + 2, clientPlayer.bottomMouseRow] == 0)
                    {
                        clientPlayer.board[clientPlayer.bottomMouseColumn, clientPlayer.bottomMouseRow] = 3;
                        clientPlayer.board[clientPlayer.bottomMouseColumn + 1, clientPlayer.bottomMouseRow] = 3;
                        clientPlayer.board[clientPlayer.bottomMouseColumn + 2, clientPlayer.bottomMouseRow] = 3;
                        clientPlayer.gameState = 4;
                    }
                    else if (clientPlayer.orientation == false && clientPlayer.bottomMouseRow < 8 && clientPlayer.board[clientPlayer.bottomMouseColumn, clientPlayer.bottomMouseRow] == 0
                        && clientPlayer.board[clientPlayer.bottomMouseColumn, clientPlayer.bottomMouseRow + 1] == 0 && clientPlayer.board[clientPlayer.bottomMouseColumn, clientPlayer.bottomMouseRow + 2] == 0)
                    {
                        clientPlayer.board[clientPlayer.bottomMouseColumn, clientPlayer.bottomMouseRow] = 3;
                        clientPlayer.board[clientPlayer.bottomMouseColumn, clientPlayer.bottomMouseRow + 1] = 3;
                        clientPlayer.board[clientPlayer.bottomMouseColumn, clientPlayer.bottomMouseRow + 2] = 3;
                        clientPlayer.gameState = 4;
                    }
                }
                else if (clientPlayer.gameState == 4)
                {
                    if (clientPlayer.orientation == true && clientPlayer.bottomMouseColumn < 8 && clientPlayer.board[clientPlayer.bottomMouseColumn, clientPlayer.bottomMouseRow] == 0
                        && clientPlayer.board[clientPlayer.bottomMouseColumn + 1, clientPlayer.bottomMouseRow] == 0 && clientPlayer.board[clientPlayer.bottomMouseColumn + 2, clientPlayer.bottomMouseRow] == 0)
                    {
                        clientPlayer.board[clientPlayer.bottomMouseColumn, clientPlayer.bottomMouseRow] = 2;
                        clientPlayer.board[clientPlayer.bottomMouseColumn + 1, clientPlayer.bottomMouseRow] = 2;
                        clientPlayer.board[clientPlayer.bottomMouseColumn + 2, clientPlayer.bottomMouseRow] = 2;
                        clientPlayer.gameState = 5;
                    }
                    else if (clientPlayer.orientation == false && clientPlayer.bottomMouseRow < 8 && clientPlayer.board[clientPlayer.bottomMouseColumn, clientPlayer.bottomMouseRow] == 0
                        && clientPlayer.board[clientPlayer.bottomMouseColumn, clientPlayer.bottomMouseRow + 1] == 0 && clientPlayer.board[clientPlayer.bottomMouseColumn, clientPlayer.bottomMouseRow + 2] == 0)
                    {
                        clientPlayer.board[clientPlayer.bottomMouseColumn, clientPlayer.bottomMouseRow] = 2;
                        clientPlayer.board[clientPlayer.bottomMouseColumn, clientPlayer.bottomMouseRow + 1] = 2;
                        clientPlayer.board[clientPlayer.bottomMouseColumn, clientPlayer.bottomMouseRow + 2] = 2;
                        clientPlayer.gameState = 5;
                    }
                }
                else if (clientPlayer.gameState == 5)
                {
                    if (clientPlayer.orientation == true && clientPlayer.bottomMouseColumn < 9 && clientPlayer.board[clientPlayer.bottomMouseColumn, clientPlayer.bottomMouseRow] == 0
                        && clientPlayer.board[clientPlayer.bottomMouseColumn + 1, clientPlayer.bottomMouseRow] == 0)
                    {
                        clientPlayer.board[clientPlayer.bottomMouseColumn, clientPlayer.bottomMouseRow] = 1;
                        clientPlayer.board[clientPlayer.bottomMouseColumn + 1, clientPlayer.bottomMouseRow] = 1;
                        //clientPlayer.gameState = 6;
                        drawBoard();
                        waitLabel.Text = "Waiting for other\nPlayer to \nset their board.";
                        rotateButton.Visible = false;
                    }
                    else if (clientPlayer.orientation == false && clientPlayer.bottomMouseRow < 9 && clientPlayer.board[clientPlayer.bottomMouseColumn, clientPlayer.bottomMouseRow] == 0
                        && clientPlayer.board[clientPlayer.bottomMouseColumn, clientPlayer.bottomMouseRow + 1] == 0)
                    {
                        clientPlayer.board[clientPlayer.bottomMouseColumn, clientPlayer.bottomMouseRow] = 1;
                        clientPlayer.board[clientPlayer.bottomMouseColumn, clientPlayer.bottomMouseRow + 1] = 1;
                        clientPlayer.gameState = 6;
                        drawBoard();
                        waitLabel.Text = "Waiting for other\nPlayer to \nset their board.";
                        rotateButton.Visible = false;
                    }
                    tradePlayerInfo();
                }
            }
        }

        /****************************************************************
        addOpponent

        Use: Adds opponent ID to client

        Parameters: None

        Returns: nothing
        ****************************************************************/
        private void addOpponent()
        {
            //Add opponentID to opponent
            string opponentIDCommand = "SELECT PlayerID FROM Player WHERE PlayerID !=" + clientPlayer.PlayerID + ";";
            try
            {
                using (SqlConnection conn = new SqlConnection(conString))
                {
                    conn.Open();
                    SqlCommand fetchOpponentID = new SqlCommand(opponentIDCommand, conn);
                    opponent.PlayerID = Convert.ToInt32(fetchOpponentID.ExecuteScalar());
                }
            }
            catch (SqlException ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        /****************************************************************
        numPlayers

        Use: Fetches number of players currently in the database

        Parameters: None

        Returns: totalPlayers (players in the database)
        ****************************************************************/
        private int numPlayers()
        {
            string playerCheck = "SELECT COUNT(PlayerID) FROM Player;";
            int totalPlayers;
            try
            {
                using(SqlConnection conn = new SqlConnection(conString)){
                    conn.Open();
                    SqlCommand p2Check = new SqlCommand(playerCheck, conn);
                    totalPlayers = Convert.ToInt32(p2Check.ExecuteScalar());
                }
                return totalPlayers;
            }
            catch (SqlException ex)
            {
                MessageBox.Show(ex.Message);
            }
            return 0;
        }

    }

    //Player class
    public class player
    {
        public int PlayerID;
        public int gameState;
        public int bottomMouseRow;
        public int bottomMouseColumn;
        public int topMouseRow;
        public int topMouseColumn;
        public bool orientation;
        public int[,] board;

        //Counting and hits against this player
        public int fiveCount, fourCount, threeCount, twoCount, oneCount;
        public int hits;
    }
}

//string exString = "CREATE TABLE Board (PlayerID int, Hits int, one1 varchar(3), one2 varchar(3), one3 varchar(3), one4 varchar(3), one5 varchar(3), two1 varchar(3), two2 varchar(3), two3 varchar(3), two4 varchar(3), three1 varchar(3), three2 varchar(3), three3 varchar(3), four1 varchar(3), four2 varchar(3), four3 varchar(3), five1 varchar(3), five2 varchar(3), primary key(PlayerID), foreign key(PlayerID) references Player(PlayerID))";
