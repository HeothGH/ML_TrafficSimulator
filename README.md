# ML_TrafficSimulator IN PROGRESS

ML_TrafficSimulator is a deterministic traffic simulation project built in Unity, connected to an AI-powered analytics server (YOLOv8). The project's goal is to study and analyze real-time traffic flow using Computer Vision methods and optimize traffic light control using Deep Reinforcement Learning.

Because of the specific, simplified vehicle graphics (low-poly/boxy cars), standard AI models struggle to identify them correctly out of the box. To solve this, the project includes a built-in pipeline to automatically collect data and fine-tune a custom object detection model.

 ![GithubVID](https://github.com/user-attachments/assets/a398b798-81ef-40af-90ae-ca4b2f9b041a)

## Key Features

* **Real-time Simulation:** A Unity 3D environment generating moving traffic with logical routing using A* pathfinding and procedural grid map generation.
* **Smart Intersections:** Intersections automatically configure their traffic light phases based on the priority of incoming roads.
* **Reinforcement Learning:** Intelligent traffic light agents trained via Unity ML-Agents to minimize traffic jams and waiting times.
* **REST API Backend:** A fast Python server built with FastAPI that receives image frames from the simulation.
* **AI Detection (YOLOv8):** Automatic vehicle counting on screenshots sent directly from the Unity CCTV cameras.
* **Automated Dataset Collection:** A built-in server mechanism that randomly saves frames to a local folder to easily gather a custom dataset for fine-tuning YOLO.

## Technologies & Architecture

* **Client / Simulation:** Unity 3D, C#, Unity ML-Agents
* **Server / Backend:** Python 3, FastAPI, Uvicorn
* **Artificial Intelligence:** Ultralytics YOLOv8, OpenCV, Pillow (PIL), Proximal Policy Optimization (PPO)

---

## Machine Learning (Reinforcement Learning)

The simulation uses Unity ML-Agents to train an intelligent agent capable of managing traffic lights dynamically. The agent is trained using the PPO (Proximal Policy Optimization) algorithm. Instead of relying on simple timers, the AI learns to optimize traffic flow by observing the environment and making decisions based on real-time data.

* **Observations:** The AI sees the normalized number of cars waiting on up to 4 incoming roads, the priority level of each road, the current active traffic light phase, and whether its decision cooldown timer is currently active.
* **Actions:** The agent has a discrete action space where it can either choose to do nothing (`0`) or change the traffic light to the next phase (`1`). To prevent erratic light flickering, the agent is restricted by a 5.0-second cooldown between phase changes.
* **Reward System (Penalties):** The agent receives a continuous negative reward for every car moving slower than 2.0 units. Penalties are heavily weighted by road priority, generating a 5x penalty for Main roads, a 2x penalty for Normal roads, and a 1x penalty for Side roads.
* **Reward System (Positives):** The agent receives a positive reward of +10 points globally every time a vehicle successfully reaches its final destination.
* **Episodes:** An episode successfully ends, and the environment resets, when all spawned vehicles in the scenario (default is 30) have either reached their destinations or been destroyed.

---

## Computer Vision & YOLOv8 Analysis

Parallel to the Reinforcement Learning environment, the project features an external Python backend to analyze the visual state of the simulation.

* **FastAPI Server:** Unity CCTV cameras send captured frames via HTTP POST requests to the `/count_cars` endpoint on the Python server.
* **Object Detection:** The server uses the pre-trained `yolov8n.pt` model to detect and count vehicles in the frame, specifically filtering for vehicle-related COCO classes.
* **Dataset Generation Loop:** Because Unity's low-poly assets differ from real-world photography, there is a 10% chance that the incoming screenshot will be saved locally to a `dataset_unity` folder with a unique timestamp. This creates an effortless dataset that can be used to retrain and fine-tune the YOLO model specifically for this Unity environment.

---

## Procedural Map Generation

To ensure the RL agent learns generalized traffic management rather than just memorizing a single layout, the simulation features a custom map generator that builds unique road networks on startup.

* **Generation Modes:** The generator supports two main structural patterns: `MiddleStraight` (creates a central, high-priority main artery with branching side roads) and `Snake` (creates a winding main road using random direction changes with inertia).
* **Priority Assignment:** As the map is drawn, roads are automatically assigned priority levels: 0 for Side roads, 1 for Normal, and 2 for Main.
* **Smart Pruning:** To prevent vehicles from getting trapped, the generator runs a cleanup pass to identify and cut off isolated or disconnected road segments.
* **Auto-Configuration:** Once the grid is instantiated, intersections automatically scan their connected roads and configure their traffic light phases based on the incoming road data.

---

## Getting Started

The project consists of two main parts that must run simultaneously: the AI server and the Unity simulation. **Note: It is highly recommended to use a virtual environment (like Anaconda or venv) for the Python scripts to avoid dependency conflicts.**

### 1. Setting up the YOLOv8 Python Server

Ensure you have Python installed (3.9+ recommended). In your terminal, navigate to the main project folder and run:

```bash
pip install fastapi uvicorn ultralytics pillow
python main.py

```

**Important Note:** On the very first run, the `ultralytics` library will automatically download the pre-trained `yolov8n.pt` model weights. Once complete, you should see a message confirming the server is listening.

### 2. Running the Unity Simulation

1. Open the project in the Unity Editor. The template scene is already set up.
2. Ensure the URL in your request-sending script is pointing to `http://127.0.0.1:8000/count_cars`.
3. Hit the **Play** button.
4. Unity will automatically generate the map, spawn traffic, and start sending frames to the server. Check your Python console to see real-time logs for vehicle counts and newly saved training images!

### 3. Running Machine Learning Training (ML-Agents)

If you want to train your own Intersection Agents from scratch using the provided `config.yaml`, you will need to use the command line.

1. Open your terminal or Anaconda Prompt.
2. Activate your ML-Agents environment:
```bash
conda activate mlagents

```


3. Navigate to the directory containing your configuration file:
```bash
cd "traffic simulator\Assets\ML-Training"

```


4. Start the training process. Replace `<YOUR_RUN_ID>` with a recognizable name for your training session:
```bash
mlagents-learn config.yaml --run-id=<YOUR_RUN_ID>

```


5. When the console displays a message asking you to start the Unity environment, press the **Play** button in the Unity Editor.
6. **Monitoring Training:** To visualize the agent's learning progress, open a new terminal window, navigate to the same folder, and start TensorBoard:
```bash
tensorboard --logdir results

```


*You can then view the graphs by opening `http://localhost:6006` in your web browser.*

## License

This project is licensed under a Custom Non-Commercial License.
You are free to download, study, and modify the code for personal and educational purposes, but **commercial use and selling are strictly prohibited**. See the `LICENSE` file for more details.
