public static class GameConstants
{
    public const string SERVER_KEY = "THIS_IS_SPARTA";

    public const float TIMEOUT_TIME = TIMEOUT_TIME_MILLISEC / 1000f;
    public const int TIMEOUT_TIME_MILLISEC = 2000;

    public const int SERVER_GET_DATA_RATE = 30;
    public const int CLIENT_GET_DATA_RATE = 30;

    public const int SERVER_SEND_DATA_RATE_MILLISEC = 30;
    public const int CLIENT_SEND_DATA_RATE_MILLISEC = 30;

    public const int COUNT_PACKETS_UNRELIABLE = 6;
    public const int LENGTH_BUFFER_SAVE_PLAYER_DATA = 32;
    public const float IGNORE_RECEIVE_DATA_TIME = 1f;

    public const int MAX_LENGTH_PLAYER_ID = 10;
    public const int MAX_LENGTH_PLAYER_NAME = 9;
    public const int MAX_PLAYERS_IN_GAME = 6;

    public const int MAX_HITS_FOR_ONE_PLAYER = 10;
    public const int MAX_HITS_FOR_PLAYERS = MAX_PLAYERS_IN_GAME * MAX_HITS_FOR_ONE_PLAYER;

    public const int DEFAULT_SIZE_WRITER = 1500;
    public const int GAME_FPS = 30;

    public const float INTERPOLATION_TIME = 0.1f;



}