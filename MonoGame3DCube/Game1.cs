using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

namespace MonoGame3DCube
{
    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        private BasicEffect _effect;
        private VertexBuffer _cubeVertexBuffer;
        private IndexBuffer _cubeIndexBuffer;
        private VertexBuffer _platformVertexBuffer;
        private Vector3 _cubePosition = new Vector3(0, 0.5f, 0);
        private Vector3 _cameraPosition;
        private Vector3 _cameraTarget;
        private const float MOVEMENT_SPEED = 5f;
        private float _cubeScale = 1f;
        private const float SCALE_SPEED = 0.5f;
        private const float MIN_SCALE = 0.1f;
        private const float MAX_SCALE = 5f;
        private Vector3 _velocity = Vector3.Zero;
        private const float GRAVITY = 19.8f;
        private const float JUMP_FORCE = 15f;
        private bool _isOnGround = true;
        private float _cameraYaw = MathHelper.Pi; // Start facing behind the cube
        private float _cameraPitch = MathHelper.PiOver4; // Start at a 45-degree angle
        private const float CAMERA_DISTANCE = 5f;
        private const float MOUSE_SENSITIVITY = 0.005f;
        private const float MIN_CAMERA_PITCH = -MathHelper.PiOver4; // Look down limit
        private const float MAX_CAMERA_PITCH = MathHelper.PiOver2 * 0.9f; // Look up limit

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = false;

        }

        protected override void Initialize()
        {
            _effect = new BasicEffect(GraphicsDevice);
            _effect.VertexColorEnabled = true;

            CreateCube();
            CreatePlatform();
            UpdateCamera();


            base.Initialize();
        }

        private void CreateCube()
        {
            VertexPositionColor[] vertices = new VertexPositionColor[8];
            vertices[0] = new VertexPositionColor(new Vector3(-0.5f, -0.5f, -0.5f), Color.Red);
            vertices[1] = new VertexPositionColor(new Vector3(0.5f, -0.5f, -0.5f), Color.Orange);
            vertices[2] = new VertexPositionColor(new Vector3(0.5f, 0.5f, -0.5f), Color.Yellow);
            vertices[3] = new VertexPositionColor(new Vector3(-0.5f, 0.5f, -0.5f), Color.Green);
            vertices[4] = new VertexPositionColor(new Vector3(-0.5f, -0.5f, 0.5f), Color.Blue);
            vertices[5] = new VertexPositionColor(new Vector3(0.5f, -0.5f, 0.5f), Color.Indigo);
            vertices[6] = new VertexPositionColor(new Vector3(0.5f, 0.5f, 0.5f), Color.Violet);
            vertices[7] = new VertexPositionColor(new Vector3(-0.5f, 0.5f, 0.5f), Color.White);

            _cubeVertexBuffer = new VertexBuffer(GraphicsDevice, typeof(VertexPositionColor), 8, BufferUsage.WriteOnly);
            _cubeVertexBuffer.SetData(vertices);

            short[] indices = new short[]
            {
                0, 1, 2, 0, 2, 3, // Front face
                1, 5, 6, 1, 6, 2, // Right face
                5, 4, 7, 5, 7, 6, // Back face
                4, 0, 3, 4, 3, 7, // Left face
                3, 2, 6, 3, 6, 7, // Top face
                4, 5, 1, 4, 1, 0  // Bottom face
            };

            _cubeIndexBuffer = new IndexBuffer(GraphicsDevice, IndexElementSize.SixteenBits, indices.Length, BufferUsage.WriteOnly);
            _cubeIndexBuffer.SetData(indices);
        }

        private void CreatePlatform()
        {
            VertexPositionColor[] platformVertices = new VertexPositionColor[6];
            platformVertices[0] = new VertexPositionColor(new Vector3(-10, 0, -10), Color.Gray);
            platformVertices[1] = new VertexPositionColor(new Vector3(10, 0, -10), Color.Gray);
            platformVertices[2] = new VertexPositionColor(new Vector3(10, 0, 10), Color.Gray);
            platformVertices[3] = new VertexPositionColor(new Vector3(-10, 0, -10), Color.Gray);
            platformVertices[4] = new VertexPositionColor(new Vector3(10, 0, 10), Color.Gray);
            platformVertices[5] = new VertexPositionColor(new Vector3(-10, 0, 10), Color.Gray);

            _platformVertexBuffer = new VertexBuffer(GraphicsDevice, typeof(VertexPositionColor), 6, BufferUsage.WriteOnly);
            _platformVertexBuffer.SetData(platformVertices);
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);
        }

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

            HandleInput(deltaTime);
            ApplyPhysics(deltaTime);
            UpdateCamera();


            base.Update(gameTime);
        }

        private void HandleInput(float deltaTime)
        {
            KeyboardState keyboardState = Keyboard.GetState();
            MouseState mouseState = Mouse.GetState();

            // Cube movement
            Vector3 movement = Vector3.Zero;
            if (keyboardState.IsKeyDown(Keys.W)) movement -= Vector3.Forward;
            if (keyboardState.IsKeyDown(Keys.S)) movement -= Vector3.Backward;
            if (keyboardState.IsKeyDown(Keys.A)) movement -= Vector3.Left;
            if (keyboardState.IsKeyDown(Keys.D)) movement -= Vector3.Right;

            // Rotate movement vector based on camera yaw
            movement = Vector3.Transform(movement, Matrix.CreateRotationY(_cameraYaw));

            // Normalize and apply movement
            if (movement != Vector3.Zero)
            {
                movement.Normalize();
                movement *= MOVEMENT_SPEED * deltaTime;

                // Move the cube in world space
                _cubePosition += movement;

                // Constrain cube to platform
                _cubePosition.X = MathHelper.Clamp(_cubePosition.X, -9.5f + _cubeScale / 2, 9.5f - _cubeScale / 2);
                _cubePosition.Z = MathHelper.Clamp(_cubePosition.Z, -9.5f + _cubeScale / 2, 9.5f - _cubeScale / 2);
            }


            // Camera rotation
            int deltaX = mouseState.X - GraphicsDevice.Viewport.Width / 2;
            int deltaY = mouseState.Y - GraphicsDevice.Viewport.Height / 2;

            _cameraYaw -= deltaX * MOUSE_SENSITIVITY;
            _cameraPitch -= deltaY * MOUSE_SENSITIVITY;

            // Clamp the pitch to prevent flipping and restrict the up/down view
            _cameraPitch = MathHelper.Clamp(_cameraPitch, MIN_CAMERA_PITCH, MAX_CAMERA_PITCH);

            Mouse.SetPosition(GraphicsDevice.Viewport.Width / 2, GraphicsDevice.Viewport.Height / 2);




            // Cube scaling
            float previousScale = _cubeScale;
            if (keyboardState.IsKeyDown(Keys.E))
            {
                _cubeScale += SCALE_SPEED * deltaTime;
            }
            if (keyboardState.IsKeyDown(Keys.Q))
            {
                _cubeScale -= SCALE_SPEED * deltaTime;
            }

            // Clamp the scale
            _cubeScale = MathHelper.Clamp(_cubeScale, MIN_SCALE, MAX_SCALE);

            // Adjust Y position when scaling to keep the bottom of the cube on the platform
            if (_isOnGround)
            {
                _cubePosition.Y += (_cubeScale - previousScale) / 2;
            }

            // Jumping
            if (_isOnGround && keyboardState.IsKeyDown(Keys.Space))
            {
                _velocity.Y = JUMP_FORCE;
                _isOnGround = false;
            }
        }

        private void ApplyPhysics(float deltaTime)
        {
            // Apply gravity
            _velocity.Y -= GRAVITY * deltaTime;

            // Apply velocity
            _cubePosition += _velocity * deltaTime;

            // Check for ground collision
            if (_cubePosition.Y - _cubeScale / 2 < 0)
            {
                _cubePosition.Y = _cubeScale / 2;
                _velocity.Y = 0;
                _isOnGround = true;
            }
        }

        private void UpdateCamera()
        {
            Vector3 cameraOffset = new Vector3(
                (float)(Math.Sin(_cameraYaw) * Math.Cos(_cameraPitch)),
                (float)Math.Sin(_cameraPitch),
                (float)(Math.Cos(_cameraYaw) * Math.Cos(_cameraPitch))
            );

            _cameraPosition = _cubePosition - cameraOffset * CAMERA_DISTANCE;
            _cameraTarget = _cubePosition + Vector3.Up * _cubeScale / 2;

            // Adjust camera height to be slightly above the cube
            _cameraPosition.Y = Math.Max(_cameraPosition.Y, _cubePosition.Y + _cubeScale);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            _effect.View = Matrix.CreateLookAt(_cameraPosition, _cameraTarget, Vector3.Up);
            _effect.Projection = Matrix.CreatePerspectiveFieldOfView(MathHelper.ToRadians(70), GraphicsDevice.Viewport.AspectRatio, 0.1f, 100f);


            // Draw platform
            _effect.World = Matrix.Identity;
            GraphicsDevice.SetVertexBuffer(_platformVertexBuffer);
            foreach (EffectPass pass in _effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                GraphicsDevice.DrawPrimitives(PrimitiveType.TriangleList, 0, 2);
            }

            // Draw cube with scaling
            _effect.World = Matrix.CreateScale(_cubeScale) * Matrix.CreateTranslation(_cubePosition);
            GraphicsDevice.SetVertexBuffer(_cubeVertexBuffer);
            GraphicsDevice.Indices = _cubeIndexBuffer;
            foreach (EffectPass pass in _effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, 12);
            }

            base.Draw(gameTime);
        }
    }
}