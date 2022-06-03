using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfUtility
{
    public class PdfTokenizer
    {
        public enum TokenKind
        {
            Eof,
            StartDictionary,
            EndDictionary,
            StartArray,
            EndArray,
            Identifier,
            Number,
            HexString,
            Symbol,
            Name,
            String
        }
        public class Token
        {
            private byte[] mBytes;

            public TokenKind Kind;
            public byte[] Bytes => mBytes;
            public bool IsEof => Kind == TokenKind.Eof;

            public Token()
            {
                Kind = TokenKind.Eof;
                mBytes = Array.Empty<byte>();
            }

            public Token(TokenKind kind, byte[] bytes)
            {
                Kind = kind;
                mBytes = bytes;
            }

            public Token(TokenKind kind, char c)
            {
                Kind = kind;
                mBytes = new byte[] { (byte)c };
            }

            public string GetString() => Encoding.ASCII.GetString(mBytes);
            public double GetDouble() => double.Parse(GetString());
            public int GetInt() => int.Parse(GetString());

            public override string ToString() => $"{Kind}::{GetString()}";

            public static readonly Token EofToken = new Token(TokenKind.Eof, Array.Empty<byte>());
        }


        bool[] mWhiteSpaceCharTable = new bool[256];
        bool[] mDelimiterCharTable = new bool[256];
        bool[] mHexCharTable = new bool[256];
        bool[] mFirstNumberCharTable = new bool[256];
        bool[] mNumberCharTable = new bool[256];

        readonly byte[] mStreamAsciiChars;// = new byte[] { 115, 116, 114, 101, 97, 109 };// Encoding.ASCII.GetBytes("stream");
        readonly byte[] mDictionaryStartChars;// = new byte[] { 60, 60 };   //<<
        readonly byte[] mDictionaryEndChars;// = new byte[] { 62, 62 };     //>>

        bool IsWhiteSpace(int c) => mWhiteSpaceCharTable[c];
        bool IsDelimiter(int c) => mDelimiterCharTable[c];
        bool IsHex(int c) => mHexCharTable[c];
        bool IsFirstNumber(int c) => mFirstNumberCharTable[c];
        bool IsNumber(int c) => mNumberCharTable[c];

        public Token CurrentToken = new();
        public long StreamLength => mStream.Length;
        public long StreamPosition
        {
            get => mStream.Position;
            set
            {
                mStream.Position = value;
                mTokenStack.Clear();
            }
        }
        public void PushToken(Token token)
        {
            mTokenStack.Push(token);
        }

        private Stack<Token> mTokenStack = new Stack<Token>();
        private int mChar;
        private Stream mStream;
        //        private long mPos = 0;

        public PdfTokenizer(Stream r)
        {
            mStream = r;
            foreach (var c in "\0\t\r\n\b\f ") mWhiteSpaceCharTable[c] = true;
            foreach (var c in "()<>[]{}/%") mDelimiterCharTable[c] = true;
            foreach (var c in "0123456789abcdefABCDEF") mHexCharTable[c] = true;
            foreach (var c in "+-.0123456789") mFirstNumberCharTable[c] = true;
            foreach (var c in ".0123456789") mNumberCharTable[c] = true;

            mStreamAsciiChars = Encoding.ASCII.GetBytes("stream");
            mDictionaryStartChars = Encoding.ASCII.GetBytes("<<");
            mDictionaryEndChars = Encoding.ASCII.GetBytes(">>");
        }

        void Back()
        {

//            if (StreamPosition < StreamLength) {
                StreamPosition--;
//            }
        }

        int GetChar()
        {
            var c = mStream.ReadByte();
            if (c < 0)
            {
                CurrentToken = Token.EofToken;
            }
            mChar = c;
            return c;
        }

        bool Skip()
        {
            while (IsWhiteSpace(mChar))
            {
                if (GetChar() < 0)
                {
                    return false;
                }
            }
            if (mChar == '%')
            {
                while (mChar != '\n' && mChar != '\r')
                {
                    if (GetChar() < 0)
                    {
                        return false;
                    }
                }
                return Skip();
            }
            return true;
        }


        public bool IsNextTokenStream()
        {
            var pos = StreamPosition;
            if (GetChar() < 0) return false;
            Skip();
            StreamPosition--;
            var buf2 = new byte[mStreamAsciiChars.Length];
            mStream.Read(buf2, 0, buf2.Length);
            if (mStreamAsciiChars.SequenceEqual(buf2))
            {
                GetChar();
                if (mChar == '\r')
                {
                    //stream識別子は\rの次は\nのはず（\r単独はつかえない）
                    GetChar();
                    if (mChar != '\n')
                    {
                        throw new Exception("need new line char after stream identifier.");
                    }
                }
                else if (mChar != '\n')
                {
                    throw new Exception("need new line char after stream identifier.");
                }
                CurrentToken = new Token(TokenKind.Identifier, mStreamAsciiChars);
                return true;
            }
            StreamPosition = pos;
            return false;
        }

        public Token GetNextToken()
        {
            if (mTokenStack.Count > 0) return mTokenStack.Pop();
            //            var sb = new StringBuilder();
            var bb = new List<byte>();
            while (true)
            {
                if (GetChar() < 0 || !Skip()) return CurrentToken;
                switch (mChar)
                {
                    case '(':
                        {
                            //                            var esc = false;
                            var parCount = 1;
                            while (true)
                            {
                                if (GetChar() < 0) throw new Exception("Unexpected eof");
                                //if (!esc)
                                //{
                                if (mChar == ')')
                                {
                                    parCount--;
                                    if (parCount == 0)
                                    {
                                        CurrentToken = new Token(TokenKind.String, bb.ToArray());
                                        return CurrentToken;
                                    }
                                }
                                if (mChar == '(') parCount++;
                                if (mChar == '\\')
                                {
                                    //エスケープシーケンス
                                    if (GetChar() < 0) throw new Exception("Unexpected eof");
                                    if (char.IsDigit((char)mChar))
                                    {
                                        var sbNumber = new StringBuilder();
                                        sbNumber.Append((char)mChar);
                                        for (var i = 0; i < 2; i++)
                                        {
                                            if (GetChar() < 0) throw new Exception("Unexpected eof");
                                            if (!char.IsDigit((char)mChar)) break;
                                            sbNumber.Append((char)mChar);
                                        }
                                        if (sbNumber.Length != 3) Back();
                                        var c = Convert.ToByte(sbNumber.ToString(), 8);
                                        bb.Add((byte)c);
                                        continue;
                                    }
                                    bb.Add((byte)((char)mChar switch
                                    {
                                        'n' => 10,
                                        'r' => 13,
                                        't' => 9,
                                        'b' => 8,
                                        'f' => 12,
                                        _ => mChar
                                    }));
                                    continue;
                                }
                                bb.Add((byte)mChar);
                            }
                        }
                    case '<':
                        if (GetChar() < 0) throw new Exception("Unexpected eof");
                        if (mChar == '<')
                        {
                            CurrentToken = new Token(TokenKind.StartDictionary, mDictionaryStartChars);
                            return CurrentToken;
                        }
                        while (true)
                        {
                            if (!Skip()) throw new Exception("Unexpected eof in hex string.");
                            if (mChar == '>')
                            {
                                if ((bb.Count & 1) != 0) bb.Add(48);//必ず偶数で奇数の場合は最後に0を付ける。
                                CurrentToken = new Token(TokenKind.HexString, bb.ToArray());
                                bb.Clear();
                                return CurrentToken;
                            }
                            if (IsHex(mChar)) bb.Add((byte)mChar);
                            if (GetChar() < 0) throw new Exception("Unexpected eof");
                        }
                    case '>':
                        if (GetChar() < 0) throw new Exception("Unexpected eof");
                        if (mChar == '>')
                        {
                            CurrentToken = new Token(TokenKind.EndDictionary, mDictionaryStartChars);
                            return CurrentToken;
                        }
                        Back();
                        CurrentToken = new Token(TokenKind.Symbol, new byte[] { 66 });
                        return CurrentToken;
                    case '/':
                        bb.Add((byte)'/');
                        while (true)
                        {
                            if (GetChar() < 0 || IsWhiteSpace(mChar) || IsDelimiter(mChar))
                            {

                                var aa = Encoding.ASCII.GetString(bb.ToArray())!;
                                if(!CurrentToken.IsEof) Back();
                                CurrentToken = new Token(TokenKind.Name, bb.ToArray());
                                bb.Clear();
                                return CurrentToken;
                            }
                            bb.Add((byte)mChar);
                        }
                    case '[':
                        CurrentToken = new Token(TokenKind.StartArray, '[');
                        return CurrentToken;
                    case ']':
                        CurrentToken = new Token(TokenKind.EndArray, ']');
                        return CurrentToken;
                    default:
                        if (char.IsLetter((char)mChar))
                        {
                            bb.Add((byte)mChar);
                            while (true)
                            {
                                if (GetChar() < 0 ||
                                    (IsDelimiter(mChar) || IsWhiteSpace(mChar)))
                                {

                                    if (!CurrentToken.IsEof) Back();
                                    var s = bb.ToArray();
                                    bb.Clear();
                                    CurrentToken = new Token(TokenKind.Identifier, s);
                                    //CurrentToken = s switch
                                    //{
                                    //    "stream" => new Token(TokenKind.StartStream, s),
                                    //    "endstream" => new Token(TokenKind.EndStream, s),
                                    //    _ => new Token(TokenKind.Identifier, s),
                                    //};
                                    return CurrentToken;
                                }
                                bb.Add((byte)mChar);
                            }
                        }
                        if (IsFirstNumber(mChar))
                        {
                            bb.Add((byte)mChar);
                            while (true)
                            {
                                if (GetChar() < 0 || !IsNumber(mChar)) // (!char.IsDigit((char)mChar) && mChar != '.'))
                                {
                                    if (!CurrentToken.IsEof) Back();
                                    CurrentToken = new Token(TokenKind.Number, bb.ToArray());
                                    bb.Clear();
                                    return CurrentToken;
                                }
                                bb.Add((byte)mChar);
                            }
                        }
                        break;
                }
            }
        }

        public byte[] ReadBytes(int length)
        {
            var bytes = new byte[length];
            mStream.Read(bytes, 0, length);
            return bytes;
        }
    }
}
