using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class GpuGameOfLife : MonoBehaviour
{
    public Font Font;
    public Material Material;

    private readonly List<Neighborhood> _neighborhoods = new();
    private readonly List<Rule> _rules = new();
    private readonly StringBuilder _stringBuilder = new();

    private Color32[] _colors;
    private int _currentNeighborhoodIndex;
    private int _currentRuleIndex;
    private bool _darkMode;
    private bool _decoy;
    private float _elapsedTime;
    private Vector3 _mousePositionOrigin;
    private Vector2 _offset;
    private Vector2 _offsetOrigin;
    private bool _paused;
    private bool _rainbowMode;
    private RenderTexture _renderTexture;
    private RenderTexture _renderTexture2;
    private int _scale = 2;
    private bool _showUI = true;
    private Texture2D _textAreaBackgroundTexture;
    private GUIStyle _textAreaStyle;
    private Texture2D _texture;
    private float _timeScale = 1;

    public Color BackgroundColor => _darkMode ? Color.black : Color.white;

    public Neighborhood CurrentNeighborhood => _neighborhoods[_currentNeighborhoodIndex];

    public Rule CurrentRule => _rules[_currentRuleIndex];

    public Color ForegroundColor => _darkMode ? Color.white : Color.black;

    protected void OnGUI()
    {
        var width = _renderTexture.width * _scale;
        var height = _renderTexture.height * _scale;
        var x = Screen.width / 2 + _offset.x - width / 2;
        var y = Screen.height / 2 + _offset.y - height / 2;
        GUI.DrawTexture(new Rect(x, y, width, height), _renderTexture);

        if (_showUI)
        {
            _stringBuilder.Clear();
            _stringBuilder.AppendLine($"[F1] to toggle UI");
            _stringBuilder.AppendLine();
            _stringBuilder.AppendLine($"[F2] to toggle dark mode: {ToOnOff(_darkMode)}");
            _stringBuilder.AppendLine($"[F3] to toggle rainbow mode: {ToOnOff(_rainbowMode)}");
            _stringBuilder.AppendLine($"[F4] to toggle decoy: {ToOnOff(_decoy)}");
            _stringBuilder.AppendLine();
            _stringBuilder.AppendLine($"[1] [2] [3] [4] [5] [6] to spawn cells");
            _stringBuilder.AppendLine();
            _stringBuilder.AppendLine($"[↑] to change rules: {CurrentRule} ({_currentRuleIndex + 1}/{_rules.Count})");
            _stringBuilder.AppendLine($"[↓] to change neighborhood: {CurrentNeighborhood} ({_currentNeighborhoodIndex + 1}/{_neighborhoods.Count})");
            _stringBuilder.AppendLine();
            _stringBuilder.AppendLine($"[←] [→] to change simulation speed: {_timeScale}x");
            _stringBuilder.AppendLine($"[SPACE] to toggle pause");
            _stringBuilder.AppendLine();
            _stringBuilder.AppendLine($"[RMB] to pan, [R] to reset");
            _stringBuilder.AppendLine($"[SCROLL WHEEL] to zoom: {_scale}x");
            GUILayout.TextArea(_stringBuilder.ToString().Trim(), _textAreaStyle);
        }

        static string ToOnOff(bool flag) => flag ? "on" : "off";
    }

    protected void Start()
    {
        _rules.Add(RuleBuilder.B(3).S(2, 3));
        _rules.Add(RuleBuilder.B(1).S(0, 1, 2, 3, 4, 5, 6, 7, 8));
        _rules.Add(RuleBuilder.B(3).S(1, 2, 3, 4, 5));
        _rules.Add(RuleBuilder.B(3, 5, 6, 7, 8).S(5, 6, 7, 8));
        _rules.Add(RuleBuilder.B(3, 4, 5).S(4, 5, 6, 7));
        _rules.Add(RuleBuilder.B(2).S());
        _rules.Add(RuleBuilder.B(2, 3, 4).S());
        _rules.Add(RuleBuilder.B(3).S(4, 5, 6, 7, 8));

        _neighborhoods.Add(new Neighborhood("Moore", 1, 1, 1, 1, 0, 1, 1, 1, 1));
        _neighborhoods.Add(new Neighborhood("Von Neumann", 0, 1, 0, 1, 0, 1, 0, 1, 0));

        _texture = new Texture2D(Screen.width / 2, Screen.height / 2)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Repeat
        };

        _renderTexture = new RenderTexture(_texture.width, _texture.height, 16)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Repeat
        };

        _renderTexture2 = new RenderTexture(_texture.width, _texture.height, 16)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Repeat
        };

        _colors = new Color32[_texture.width * _texture.height];

        _textAreaBackgroundTexture = new Texture2D(1, 1);

        _textAreaStyle = new GUIStyle
        {
            font = Font,
            fontSize = 18,
            padding = new RectOffset(10, 10, 10, 10)
        };
        _textAreaStyle.normal.background = _textAreaBackgroundTexture;

        Shader.SetGlobalVector("_MainTexSize", new Vector4(1.0f / _texture.width, 1.0f / _texture.height));

        InvalidateTheme();
    }

    protected void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1))
            _showUI ^= true;

        if (Input.GetKeyDown(KeyCode.F2))
        {
            _darkMode ^= true;
            InvalidateTheme();
        }

        if (Input.GetKeyDown(KeyCode.F3))
            _rainbowMode ^= true;

        if (Input.GetKeyDown(KeyCode.F4))
            _decoy ^= true;

        if (Input.GetKeyDown(KeyCode.Space))
            _paused ^= true;

        if (Input.GetKeyDown(KeyCode.R))
            _offset = new Vector2();

        if (Input.GetKeyDown(KeyCode.Alpha1))
            FillCenter(0);

        if (Input.GetKeyDown(KeyCode.Alpha2))
            FillCenter(1);

        if (Input.GetKeyDown(KeyCode.Alpha3))
            FillRandom(0.9999f);

        if (Input.GetKeyDown(KeyCode.Alpha4))
            FillRandom(0.95f);

        if (Input.GetKeyDown(KeyCode.Alpha5))
            FillRandom(0.8f);

        if (Input.GetKeyDown(KeyCode.Alpha6))
            FillRandom(0.65f);

        if (Input.GetKeyDown(KeyCode.RightArrow))
            _timeScale *= 2f;

        if (Input.GetKeyDown(KeyCode.LeftArrow))
            _timeScale /= 2f;

        if (Input.GetKeyDown(KeyCode.UpArrow))
            _currentRuleIndex = (_currentRuleIndex + 1) % _rules.Count;

        if (Input.GetKeyDown(KeyCode.DownArrow))
            _currentNeighborhoodIndex = (_currentNeighborhoodIndex + 1) % _neighborhoods.Count;

        if (Input.mouseScrollDelta.y > 0)
            _scale *= 2;

        if (Input.mouseScrollDelta.y < 0)
            _scale /= 2;

        _scale = Mathf.Max(_scale, 1);

        if (Input.GetKeyDown(KeyCode.Mouse1))
        {
            _offsetOrigin = _offset;
            _mousePositionOrigin = Input.mousePosition;
        }

        if (Input.GetKey(KeyCode.Mouse1))
        {
            _offset = _offsetOrigin + Vector2.Scale(Input.mousePosition - _mousePositionOrigin, new Vector2(1, -1));
        }

        if (!_paused)
        {
            _elapsedTime += Time.deltaTime * _timeScale;
            if (_elapsedTime > 0.05f)
            {
                Graphics.Blit(_renderTexture, _renderTexture2, Material);
                (_renderTexture2, _renderTexture) = (_renderTexture, _renderTexture2);
                _elapsedTime = 0f;
            }
        }

        Shader.SetGlobalFloatArray("_B", CurrentRule.B);
        Shader.SetGlobalFloatArray("_S", CurrentRule.S);
        Shader.SetGlobalFloatArray("_N", CurrentNeighborhood.N);
        Shader.SetGlobalColor("_ForegroundColor", GetForegroundColor());
        Shader.SetGlobalFloat("_Decoy", _decoy ? 1 : 0);
    }

    private void Fill(Func<int, Color32> getColor)
    {
        for (var i = 0; i < _colors.Length; i++)
            _colors[i] = getColor(i);

        _texture.SetPixels32(_colors);
        _texture.Apply();

        Graphics.Blit(_texture, _renderTexture);
    }

    private void FillCenter(int radius)
    {
        Fill(i =>
        {
            var y = i / _texture.width;
            var x = i - y * _texture.width;

            var dx = Mathf.Abs(x - _texture.width / 2);
            var dy = Mathf.Abs(y - _texture.height / 2);

            return dx <= radius && dy <= radius ? GetForegroundColor() : Color.clear;
        });
    }

    private void FillEmpty()
    {
        Fill(i => Color.clear);
    }

    private void FillRandom(float p)
    {
        Fill(i => UnityEngine.Random.value > p ? GetForegroundColor() : Color.clear);
    }

    private Color GetForegroundColor()
    {
        return _rainbowMode ? Color.HSVToRGB(Time.time % 1, 1, 1) : ForegroundColor;
    }

    private void InvalidateTheme()
    {
        GetComponent<Camera>().backgroundColor = BackgroundColor;

        _textAreaBackgroundTexture.SetPixels32(new Color32[] { BackgroundColor });
        _textAreaBackgroundTexture.Apply();

        _textAreaStyle.normal.textColor = ForegroundColor;

        FillEmpty();
    }

    public class Neighborhood
    {
        public readonly float[] N = new float[9];

        private readonly string _name;

        public Neighborhood(string name, params float[] n)
        {
            _name = name;
            Array.Copy(n, N, n.Length);
        }

        public override string ToString()
        {
            return _name;
        }
    }

    public class Rule
    {
        public readonly float[] B = new float[9];
        public readonly float[] S = new float[9];

        private readonly string _name;

        public Rule(int[] b, int[] s)
        {
            _name = "B";
            Add(b, B, ref _name);
            _name += "/S";
            Add(s, S, ref _name);

            static void Add(int[] input, float[] output, ref string name)
            {
                var hashSet = new HashSet<int>(input);
                for (var i = 0; i < output.Length; i++)
                {
                    if (hashSet.Contains(i))
                    {
                        output[i] = 1;
                        name += i;
                    }
                }
            }
        }

        public override string ToString()
        {
            return _name;
        }
    }

    public class RuleBuilder
    {
        private int[] _b;

        public static RuleBuilder B(params int[] b)
        {
            return new RuleBuilder { _b = b };
        }

        public Rule S(params int[] s)
        {
            return new Rule(_b, s);
        }
    }
}