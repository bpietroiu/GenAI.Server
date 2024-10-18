using GenAI.Server.Jinja.Expressions;

namespace GenAI.Server.Jinja
{

    public static class Parser
    {
        private static List<Token>? _tokens;
        private static int _currentTokenIndex;

        public static ProgramNode Parse(List<Token> tokens)
        {
            _tokens = tokens;
            _currentTokenIndex = 0;
            List<Statement> body = new();

            while (_currentTokenIndex < _tokens.Count)
            {
                body.Add(ParseAny());
            }

            return new ProgramNode { Body = body };
        }

        private static Statement ParseAny()
        {
            Token currentToken = _tokens[_currentTokenIndex];

            return currentToken.Type switch
            {
                TokenTypes.Text => ParseText(),
                TokenTypes.OpenStatement => ParseJinjaStatement(),
                TokenTypes.OpenExpression => ParseJinjaExpression(),
                _ => throw new SyntaxError($"Unexpected token type: {currentToken.Type}"),
            };
        }

        private static bool Is(params string[] types)
        {
            if (_currentTokenIndex + types.Length > _tokens.Count)
            {
                return false;
            }

            for (int i = 0; i < types.Length; i++)
            {
                if (_tokens[_currentTokenIndex + i].Type != types[i])
                {
                    return false;
                }
            }

            return true;
        }

        private static Token Expect(string type, string errorMessage)
        {
            return _currentTokenIndex >= _tokens.Count || _tokens[_currentTokenIndex].Type != type
                ? throw new SyntaxError($"Parser Error: {errorMessage}, found {_tokens[_currentTokenIndex].Type} {_tokens[_currentTokenIndex].Value} token at [{_tokens[_currentTokenIndex].Line}:{_tokens[_currentTokenIndex].Column}] ")
                : _tokens[_currentTokenIndex++];
        }

        private static Statement ParseText()
        {
            Token token = Expect(TokenTypes.Text, "Expected text token");
            return new StringLiteral(token.Value);
        }

        private static Statement ParseJinjaStatement()
        {
            _ = Expect(TokenTypes.OpenStatement, "Expected opening statement token");
            Statement result;

            switch (_tokens[_currentTokenIndex].Type)
            {
                case TokenTypes.Set:
                    _currentTokenIndex++;
                    result = ParseSetStatement();
                    _ = Expect(TokenTypes.CloseStatement, "Expected closing statement token");
                    break;
                case TokenTypes.If:
                    _currentTokenIndex++;
                    result = ParseIfStatement();
                    _ = Expect(TokenTypes.OpenStatement, "Expected {% token");
                    _ = Expect(TokenTypes.EndIf, "Expected endif token");
                    _ = Expect(TokenTypes.CloseStatement, "Expected %} token");
                    break;
                case TokenTypes.Macro:
                    _currentTokenIndex++;
                    result = ParseMacroStatement();
                    _ = Expect(TokenTypes.OpenStatement, "Expected {% token");
                    _ = Expect(TokenTypes.EndMacro, "Expected endmacro token");
                    _ = Expect(TokenTypes.CloseStatement, "Expected %} token");
                    break;
                case TokenTypes.For:
                    _currentTokenIndex++;
                    result = ParseForStatement();
                    _ = Expect(TokenTypes.OpenStatement, "Expected {% token");
                    _ = Expect(TokenTypes.EndFor, "Expected endfor token");
                    _ = Expect(TokenTypes.CloseStatement, "Expected %} token");
                    break;
                default:
                    throw new SyntaxError($"Unknown statement type: {_tokens[_currentTokenIndex].Type}");
            }

            return result;
        }

        private static Statement ParseJinjaExpression()
        {
            _ = Expect(TokenTypes.OpenExpression, "Expected opening expression token");
            Expression result = ParseExpression();
            _ = Expect(TokenTypes.CloseExpression, "Expected closing expression token");
            return result;
        }

        private static Statement ParseSetStatement()
        {
            Expression left = ParseExpression();
            if (Is(TokenTypes.Equals))
            {
                _currentTokenIndex++;
                Statement value = ParseSetStatement();
                return new SetStatement(left, value);
            }

            return left;
        }

        private static IfStatement ParseIfStatement()
        {
            Expression test = ParseExpression();
            _ = Expect(TokenTypes.CloseStatement, "Expected closing statement token");
            List<Statement> body = new();
            List<Statement> alternate = new();

            while (!Is(TokenTypes.OpenStatement, TokenTypes.ElseIf) && !Is(TokenTypes.OpenStatement, TokenTypes.Else) && !Is(TokenTypes.OpenStatement, TokenTypes.EndIf))
            {
                body.Add(ParseAny());
            }

            if (Is(TokenTypes.OpenStatement, TokenTypes.ElseIf))
            {
                _currentTokenIndex += 2;
                alternate.Add(ParseIfStatement());
            }
            else if (Is(TokenTypes.OpenStatement, TokenTypes.Else))
            {
                _currentTokenIndex += 2;
                _ = Expect(TokenTypes.CloseStatement, "Expected closing statement token");
                while (!Is(TokenTypes.OpenStatement, TokenTypes.EndIf))
                {
                    alternate.Add(ParseAny());
                }
            }

            return new IfStatement(test, body, alternate);
        }

        private static MacroStatement ParseMacroStatement()
        {
            if (ParsePrimaryExpression() is not IdentifierExpression name)
            {
                throw new SyntaxError("Expected identifier following macro statement");
            }

            List<Expression> args = ParseArgs();
            _ = Expect(TokenTypes.CloseStatement, "Expected closing statement token");

            List<Statement> body = new();
            while (!Is(TokenTypes.OpenStatement, TokenTypes.EndMacro))
            {
                body.Add(ParseAny());
            }

            return new MacroStatement(name, args, body);
        }

        private static ForStatement ParseForStatement()
        {
            Expression loopVar = ParseExpressionSequence(true);
            if (loopVar is not (IdentifierExpression or TupleLiteral))
            {
                throw new SyntaxError("Expected identifier/tuple for the loop variable");
            }

            _ = Expect(TokenTypes.In, "Expected 'in' keyword following loop variable");
            Expression iterable = ParseExpression();
            _ = Expect(TokenTypes.CloseStatement, "Expected closing statement token");

            List<Statement> body = new();
            List<Statement> defaultBlock = new();

            while (!Is(TokenTypes.OpenStatement, TokenTypes.EndFor) && !Is(TokenTypes.OpenStatement, TokenTypes.Else))
            {
                body.Add(ParseAny());
            }

            if (Is(TokenTypes.OpenStatement, TokenTypes.Else))
            {
                _currentTokenIndex += 2;
                _ = Expect(TokenTypes.CloseStatement, "Expected closing statement token");
                while (!Is(TokenTypes.OpenStatement, TokenTypes.EndFor))
                {
                    defaultBlock.Add(ParseAny());
                }
            }

            return new ForStatement(loopVar, iterable, body, defaultBlock);
        }

        private static Expression ParseExpressionSequence(bool primary = false)
        {

            Func<Expression> fn = ParseExpression;
            if (primary)
            {
                fn = ParsePrimaryExpression;
            }
            List<Expression> expressions = new()
            { fn() };

            if (Is(TokenTypes.Comma))
            {
                _currentTokenIndex++;
                while (!Is(TokenTypes.CloseParen) && !Is(TokenTypes.CloseSquareBracket) && !Is(TokenTypes.In))
                {
                    expressions.Add(fn());
                    if (Is(TokenTypes.Comma))
                    {
                        _currentTokenIndex++;
                    }
                }
            }

            return expressions.Count == 1 ? expressions[0] : new TupleLiteral(expressions);
        }

        private static Expression ParseExpression()
        {
            return ParseIfExpression();
        }
        private static Expression ParseTestExpression()
        {
            Expression left = ParseAdditiveExpression();  // Start with the left-hand side (e.g., `system_message`)

            if (Is(TokenTypes.Is))
            {
                _currentTokenIndex++;  // Move past the `is`

                bool negate = Is(TokenTypes.Not);
                if (negate)
                {
                    _currentTokenIndex++;  // Handle `is not` for negation
                }

                // Parse the test type (e.g., `defined`)
                Expression testType = ParsePrimaryExpression();
                if (testType is IdentifierExpression identifier)
                {
                    return new TestExpression(left, negate, identifier);  // Handle `is defined`
                }
                else
                if (testType is NullLiteral)
                {
                    return new TestExpression(left, negate, new IdentifierExpression("none"));  // Handle `is defined`
                }
                else
                {
                    throw new SyntaxError($"Expected `defined` after `is` found {testType.ToCode()} ");
                }
            }

            return left;
        }

        private static Expression ParseIfExpression()
        {
            Expression expression = ParseLogicalOrExpression();
            if (Is(TokenTypes.If))
            {
                _currentTokenIndex++;
                Expression test = ParseLogicalOrExpression();
                if (Is(TokenTypes.Else))
                {
                    _currentTokenIndex++;
                    Expression alternate = ParseLogicalOrExpression();
                    return new IfStatement(test, [expression], [alternate]);
                }

                return new IfStatement(test, [expression], []);
            }

            return expression;
        }

        private static Expression ParseLogicalOrExpression()
        {
            Expression left = ParseLogicalAndExpression();

            while (Is(TokenTypes.Or))
            {
                Token operatorToken = _tokens[_currentTokenIndex];
                _currentTokenIndex++;
                Expression right = ParseLogicalAndExpression();
                left = new BinaryExpression(operatorToken.Value, left, right);
            }

            return left;
        }

        private static Expression ParseLogicalAndExpression()
        {
            Expression left = ParseLogicalNegationExpression();

            while (Is(TokenTypes.And))
            {
                Token operatorToken = _tokens[_currentTokenIndex];
                _currentTokenIndex++;
                Expression right = ParseLogicalNegationExpression();
                left = new BinaryExpression(operatorToken.Value, left, right);
            }

            return left;
        }

        private static Expression ParseLogicalNegationExpression()
        {
            if (Is(TokenTypes.Not))
            {
                Token operatorToken = _tokens[_currentTokenIndex];
                _currentTokenIndex++;
                Expression argument = ParseLogicalNegationExpression();
                return new UnaryExpression(operatorToken.Value, argument);
            }

            return ParseComparisonExpression();
        }

        private static Expression ParseComparisonExpression()
        {
            Expression left = ParseTestExpression();

            while (Is(TokenTypes.ComparisonBinaryOperator) || Is(TokenTypes.In))
            {
                Token operatorToken = _tokens[_currentTokenIndex];
                _currentTokenIndex++;
                Expression right = ParseTestExpression();
                left = new BinaryExpression(operatorToken.Value, left, right);
            }

            return left;
        }

        private static Expression ParseAdditiveExpression()
        {
            Expression left = ParseMultiplicativeExpression();

            while (Is(TokenTypes.AdditiveBinaryOperator))
            {
                Token operatorToken = _tokens[_currentTokenIndex];
                _currentTokenIndex++;
                Expression right = ParseMultiplicativeExpression();
                left = new BinaryExpression(operatorToken.Value, left, right);
            }

            return left;
        }

        private static Expression ParseMultiplicativeExpression()
        {
            Expression left = ParseCallMemberExpression();

            while (Is(TokenTypes.MultiplicativeBinaryOperator))
            {
                Token operatorToken = _tokens[_currentTokenIndex];
                _currentTokenIndex++;
                Expression right = ParseCallMemberExpression();
                left = new BinaryExpression(operatorToken.Value, left, right);
            }

            return left;
        }

        private static Expression ParseCallMemberExpression()
        {
            Expression member = ParseMemberExpression();

            var call = Is(TokenTypes.OpenParen) ? ParseCallExpression(member) : member;
            return ParseFilterExpression(call);
        }
        private static Expression ParseMemberExpressionArgumentsList(Expression @object)
        {
            List<Expression> slices = new();
            bool isSlice = false;

            // Loop to handle multiple slice parts
            while (!Is(TokenTypes.CloseSquareBracket))
            {
                if (Is(TokenTypes.Colon)) // Slice separator
                {
                    slices.Add(null); // Add a null if nothing is provided before the colon
                    _currentTokenIndex++;
                    isSlice = true;
                }
                else
                {
                    slices.Add(ParseExpression()); // Parse the start, stop, or step expression
                    if (Is(TokenTypes.Colon))
                    {
                        _currentTokenIndex++;
                        isSlice = true;
                    }
                }
            }

            if (slices.Count == 0)
            {
                throw new SyntaxError("Expected at least one argument for member or slice expression");
            }

            if (isSlice) // If it's a slice, we can construct a `SliceExpression`
            {
                if (slices.Count > 3)
                {
                    throw new SyntaxError("Expected 0-3 arguments for slice expression");
                }

                Expression? start = slices.ElementAtOrDefault(0);
                Expression? stop = slices.ElementAtOrDefault(1);
                Expression? step = slices.ElementAtOrDefault(2);
                return new SliceExpression(@object, start, stop, step);
            }

            return slices[0]; // If it's not a slice, return the single expression
        }

        private static Expression ParseMemberExpression()
        {
            Expression objectExpr = ParsePrimaryExpression();

            while (Is(TokenTypes.Dot) || Is(TokenTypes.OpenSquareBracket))
            {
                Token operatorToken = _tokens[_currentTokenIndex];
                _currentTokenIndex++;

                Expression property;
                bool computed = operatorToken.Type == TokenTypes.OpenSquareBracket;

                if (computed) // Array access or slice
                {
                    property = ParseMemberExpressionArgumentsList(objectExpr); // Handle slicing like `messages[2:]`
                    _ = Expect(TokenTypes.CloseSquareBracket, "Expected closing square bracket");
                }
                else // Dot notation for accessing properties
                {
                    property = ParsePrimaryExpressionBase(); // Parse the property identifier
                    if (property is not IdentifierExpression)
                    {
                        throw new SyntaxError($"Expected identifier following dot operator, got {property.Type}");
                    }
                }

                objectExpr = new MemberExpression(objectExpr, property, computed);
            }

            return objectExpr;
        }
        private static Expression ParseCallExpression(Expression callee)
        {
            List<Expression> args = ParseArgs();

            if (Is(TokenTypes.OpenParen))
            {
                callee = ParseCallExpression(callee);
            }

            return new CallExpression(callee, args);
        }

        private static List<Expression> ParseArgs()
        {
            _ = Expect(TokenTypes.OpenParen, "Expected opening parenthesis for arguments list");

            List<Expression> args = new();

            while (!Is(TokenTypes.CloseParen))
            {
                var arg = ParseExpression();

                if (Is(TokenTypes.Comma))
                {
                    _currentTokenIndex++;
                }
                else
                if (Is(TokenTypes.Equals))
                {
                    // If it's a keyword argument, we parse it as a KeywordArgumentExpression
                    _currentTokenIndex++;
                    var value = ParseExpression();  // Parse the value assigned to the keyword

                    arg = new KeywordArgumentExpression((IdentifierExpression)arg, value);
                    if (Is(TokenTypes.Comma))
                    {
                        _currentTokenIndex++;
                    }
                }
                args.Add(arg);
            }

            _ = Expect(TokenTypes.CloseParen, "Expected closing parenthesis for arguments list");
            return args;
        }
        private static Expression ParsePrimaryExpression()
        {
            Expression baseExpr = ParsePrimaryExpressionBase();
            return ParseFilterExpression(baseExpr);
        }
        private static Expression ParsePrimaryExpressionBase()
        {
            Token currentToken = _tokens[_currentTokenIndex];

            switch (currentToken.Type)
            {
                case TokenTypes.NumericLiteral:
                    _currentTokenIndex++;
                    return new NumericLiteral(int.Parse(currentToken.Value));

                case TokenTypes.StringLiteral:
                    _currentTokenIndex++;
                    return new StringLiteral(currentToken.Value);

                case TokenTypes.BooleanLiteral:
                    _currentTokenIndex++;
                    return new BooleanLiteral(bool.Parse(currentToken.Value.ToLower()));

                case TokenTypes.NullLiteral:
                    _currentTokenIndex++;
                    return new NullLiteral();

                case TokenTypes.Identifier:
                    _currentTokenIndex++;
                    return new IdentifierExpression(currentToken.Value);

                case TokenTypes.OpenParen:
                    _currentTokenIndex++;
                    Expression expression = ParseExpressionSequence();
                    _ = Expect(TokenTypes.CloseParen, "Expected closing parenthesis");
                    return expression;

                case TokenTypes.OpenSquareBracket:
                    _currentTokenIndex++;
                    List<Expression> arrayElements = new();
                    while (_tokens[_currentTokenIndex].Type != TokenTypes.CloseSquareBracket)
                    {
                        arrayElements.Add(ParseExpression());
                        if (Is(TokenTypes.Comma))
                        {
                            _currentTokenIndex++;
                        }
                    }
                    _ = Expect(TokenTypes.CloseSquareBracket, "Expected closing square bracket");
                    return new ArrayLiteral(arrayElements);

                case TokenTypes.OpenCurlyBracket:
                    _currentTokenIndex++;
                    Dictionary<Expression, Expression> objectLiteral = new();
                    while (!Is(TokenTypes.CloseCurlyBracket))
                    {
                        Expression key = ParseExpression();
                        _ = Expect(TokenTypes.Colon, "Expected colon between key and value in object literal");
                        Expression value = ParseExpression();
                        objectLiteral.Add(key, value);
                        if (Is(TokenTypes.Comma))
                        {
                            _currentTokenIndex++;
                        }
                    }
                    _ = Expect(TokenTypes.CloseCurlyBracket, "Expected closing curly bracket");
                    return new ObjectLiteral(objectLiteral);

                default:
                    throw new SyntaxError($"Unexpected token: {currentToken.Type} [{currentToken.Line}:{currentToken.Column}]");
            }
        }

        private static Expression ParseFilterExpression(Expression baseExpression)
        {
            // Loop to handle multiple filters chained together
            while (Is(TokenTypes.Pipe))  // Check if there is a pipe (|) indicating a filter
            {
                _currentTokenIndex++;  // Move past the pipe token

                var filterName = ParsePrimaryExpression();  // The filter name (e.g., `upper`, `lower`)
                if (!(filterName is IdentifierExpression filterIdentifier))
                {
                    throw new SyntaxError("Expected a filter name after the pipe (|)");
                }

                // Check for optional arguments to the filter
                List<Expression> filterArgs = new List<Expression>();
                if (Is(TokenTypes.OpenParen))
                {
                    filterArgs.AddRange(ParseArgs());
                    _currentTokenIndex--;
                    Expect(TokenTypes.CloseParen, "Expected closing parenthesis for filter arguments");
                }

                // Create a FilterExpression node to represent this filter application
                baseExpression = new FilterExpression(baseExpression, filterIdentifier, filterArgs);
            }

            return baseExpression;  // Return the base expression with all filters applied
        }
    }

}
