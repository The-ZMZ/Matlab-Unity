# CLEAN MY ROOM!

## Project Overview
Clean My Room is a 3D educational game for children developed using MATLAB and Unity.

The game teaches children through three interactive activities:
1. Color Sorting.
2. Taste Sorting.
3. Number and Dot Matching.

The player explores a 3D bedroom, completes activities using hand gestures, earns three stars, and receives encouragement from the father character.

## Technologies
- MATLAB: Computer Graphics, 2D and 3D transformations, Hand Tracking, Gesture Recognition, Activity Validation, and Game Logic.
- Unity: 3D Environment, Characters, Animations, UI, Audio, and Player Movement.
- TCP/IP and JSON: MATLAB–Unity Communication.

Python is not used in this project.

## Project Structure
- MATLAB: Computational algorithms and game logic.
- Unity: Interactive 3D game.
- Shared_Data: Shared resources and communication specifications.
- Documentation: Technical documentation and test results.
- Deliverables: Screenshots, videos, and final reports.

## Main Activities
- Color Sorting: Match objects to their corresponding colored baskets.
- Taste Sorting: Classify food into Sweet, Sour, Bitter, and Salty.
- Math Activity: Match numbers with groups containing the same number of dots.

## Hand Interaction
The game uses webcam-based hand tracking in MATLAB.
The player uses a Pinch gesture (thumb and index finger) to grab, move, and release objects.

## System Architecture
MATLAB is responsible for computational processing, hand gesture recognition, activity validation, and game progress logic.

Unity is responsible for displaying the 3D environment and presenting the game interactions.

The two applications communicate through TCP/IP using JSON messages.

## Project Goal
Develop a complete interactive educational game while demonstrating Computer Graphics concepts and MATLAB-based image processing.
