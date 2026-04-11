import { Routes, Route } from "react-router-dom";
import HomePage from "./pages/HomePage";
import DashboardPage from "./pages/DashboardPage";
import GraphPage from "./pages/GraphPage";
import RoadmapPage from "./pages/RoadmapPage";
import TransformPage from "./pages/TransformPage";

export default function App() {
  return (
    <div className="min-h-screen bg-gray-950 text-gray-100">
      <Routes>
        <Route path="/" element={<HomePage />} />
        <Route path="/dashboard/:projectId" element={<DashboardPage />} />
        <Route path="/graph/:projectId" element={<GraphPage />} />
        <Route path="/roadmap/:projectId" element={<RoadmapPage />} />
        <Route path="/transform/:projectId" element={<TransformPage />} />
      </Routes>
    </div>
  );
}
