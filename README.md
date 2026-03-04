# ML_TrafficSimulator IN PROGRESS

ML_TrafficSimulator is a deterministic (seed based) traffic simulation project built in Unity, connected to an AI-powered analytics server (YOLOv8). The project's goal is to study and analyze real-time traffic flow using Computer Vision methods and optimize traffic light control using Deep Reinforcement Learning.

Because of the specific, simplified vehicle graphics (low-poly/boxy cars), standard AI models struggle to identify them correctly out of the box. To solve this, the project includes a built-in pipeline to automatically collect data and fine-tune a custom object detection model.

 ![GithubVID](https://github.com/user-attachments/assets/a398b798-81ef-40af-90ae-ca4b2f9b041a)

## Key Features

* **Real-time Simulation:** A Unity 3D environment generating moving traffic with logical routing (A* pathfinding) and procedural grid map generation.
* **Smart Intersections:** Intersections automatically configure their traffic light phases based on the priority of incoming roads.
* **Reinforcement Learning:** Intelligent traffic light agents trained via Unity ML-Agents to minimize traffic jams and waiting times.
* **REST API Backend:** A fast Python server built with `FastAPI` that receives image frames from the simulation.
* **AI Detection (YOLOv8):** Automatic vehicle counting on screenshots sent directly from the Unity CCTV cameras.
* **Automated Dataset Collection:** A built-in server mechanism that randomly saves frames to a local folder to easily gather a custom dataset for fine-tuning YOLO.

## Technologies & Architecture

* **Client / Simulation:** Unity 3D, C#, Unity ML-Agents
* **Server / Backend:** Python 3, FastAPI, Uvicorn
* **Artificial Intelligence:** Ultralytics YOLOv8, OpenCV, Pillow (PIL), Proximal Policy Optimization (PPO)

---

## Machine Learning (Reinforcement Learning)

The simulation uses **Unity ML-Agents** to train an intelligent `IntersectionAgent` capable of managing traffic lights dynamically. The agent is trained using the PPO (Proximal Policy Optimization) algorithm.

Instead of relying on simple timers, the AI learns to optimize traffic flow by observing the environment and making decisions based on real-time data:

* **Observations (What the AI sees):**
* The number of cars waiting on up to 4 incoming roads (normalized).
* The priority level of each incoming road (0 = Side, 1 = Normal, 2 = Main).
* The current active traffic light phase.
* Whether the agent's decision cooldown timer is currently active.


* **Actions (What the AI can do):** * The agent has a discrete action space. It can either choose to do nothing (`0`) or change the traffic light to the next phase (`1`).
* To prevent erratic light flickering, the agent is restricted by a 5.0-second cooldown between phase changes.


* **Reward System (How the AI learns):**
* **Penalties:** The agent receives a continuous negative reward (penalty) for every car that is stuck in traffic (moving slower than 2.0 units).
* **Priority Multipliers:** Penalties are heavily weighted by road priority. A car waiting on a Main Road generates a 5x penalty, a Normal Road generates a 2x penalty, and a Side Road generates a 1x penalty.
* **Rewards:** The agent receives a positive reward (+10 points) globally every time a vehicle successfully reaches its final destination.


* **Episodes:** An episode successfully ends, and the environment resets, when all spawned vehicles in the scenario (default is 30) have reached their destinations.

---

## Computer Vision & YOLOv8 Analysis

Parallel to the Reinforcement Learning environment, the project features an external Python backend to analyze the visual state of the simulation.

* **FastAPI Server:** Unity CCTV cameras send captured frames via HTTP POST requests to the `/count_cars` endpoint on the Python server.
* **Object Detection:** The server uses the pre-trained `yolov8n.pt` model to detect and count vehicles in the frame. It specifically filters for vehicle-related COCO classes (cars, motorcycles, buses, trucks).
* **Dataset Generation Loop:** Because Unity's low-poly assets differ from real-world photography, the default YOLOv8 model might struggle. To solve this, the server features an automated data collection pipeline. There is a 10% chance (1 in 10 frames) that the incoming screenshot will be saved locally to a `dataset_unity` folder with a unique timestamp. This creates an effortless, organic dataset that can be used to retrain and fine-tune the YOLO model specifically for this Unity environment.

---

## Getting Started

The project consists of two main parts that must run simultaneously: the AI server and the Unity simulation. **Note: It is highly recommended to use a virtual environment (like Anaconda or venv) for the Python scripts to avoid dependency conflicts.**

### 1. Setting up the YOLOv8 Python Server

The Python server acts as the brain for the computer vision pipeline, receiving image frames from Unity.

Ensure you have Python installed (3.9+ recommended). In your terminal, navigate to the main project folder and run:

```bash
# Install the required libraries
pip install fastapi uvicorn ultralytics pillow

# Run the server
python main.py

```

*(Alternatively, you can run it directly via Uvicorn: `uvicorn main:app --host 0.0.0.0 --port 8000`)*

**Important Note:** On the very first run, the `ultralytics` library will automatically download the pre-trained `yolov8n.pt` model weights. Once complete, you should see a message confirming the server is listening on `http://localhost:8000` (or `0.0.0.0:8000`).

### 2. Running the Unity Simulation

1. Open the project in the Unity Editor. The template scene is already set up.
2. Ensure the URL in your request-sending script (attached to your camera or manager) is pointing to `http://127.0.0.1:8000/count_cars`.
3. Hit the **Play** button.
4. Unity will automatically generate the map, spawn traffic, and start sending frames to the server. Check your Python console to see real-time logs for vehicle counts and newly saved training images!

## License

This project is licensed under a Custom Non-Commercial License.
You are free to download, study, and modify the code for personal and educational purposes, but **commercial use and selling are strictly prohibited**. See the `LICENSE` file for more details.
