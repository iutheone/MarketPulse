import ReactDOM from "react-dom/client";
import { BrowserRouter, Navigate, Route, Routes } from "react-router-dom";
import { Layout } from "./ui";
import { DashboardPage } from "./pages/Dashboard";
import { StockDetailsPage } from "./pages/StockDetails";
import { AnomalyDetailsPage } from "./pages/AnomalyDetails";
import { WatchlistPage } from "./pages/Watchlist";
import { DetectionRulesPage } from "./pages/DetectionRules";
import { HealthPage } from "./pages/Health";
import { BacktestsPage } from "./pages/Backtests";
import "./styles.css";

ReactDOM.createRoot(document.getElementById("root")!).render(
  <BrowserRouter>
      <Routes>
        <Route element={<Layout />}>
          <Route path="/" element={<DashboardPage />} />
          <Route path="/stocks/:symbol" element={<StockDetailsPage />} />
          <Route path="/anomalies/:id" element={<AnomalyDetailsPage />} />
          <Route path="/watchlists" element={<WatchlistPage />} />
          <Route path="/rules" element={<DetectionRulesPage />} />
          <Route path="/health" element={<HealthPage />} />
          <Route path="/backtests" element={<BacktestsPage />} />
          <Route path="*" element={<Navigate to="/" replace />} />
        </Route>
      </Routes>
    </BrowserRouter>
);
