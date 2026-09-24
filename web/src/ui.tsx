import { NavLink, Outlet } from "react-router-dom";

export function Layout() {
  return (
    <div className="shell">
      <nav className="side">
        <h1>MarketPulse</h1>
        <p>Unusual activity scanner. Not a trading product.</p>
        <NavLink to="/" end className={({ isActive }) => (isActive ? "active" : "")}>
          Dashboard
        </NavLink>
        <NavLink to="/watchlists" className={({ isActive }) => (isActive ? "active" : "")}>
          Watchlists
        </NavLink>
        <NavLink to="/rules" className={({ isActive }) => (isActive ? "active" : "")}>
          Detection rules
        </NavLink>
        <NavLink to="/health" className={({ isActive }) => (isActive ? "active" : "")}>
          System health
        </NavLink>
        <NavLink to="/backtests" className={({ isActive }) => (isActive ? "active" : "")}>
          Replay / backtest
        </NavLink>
      </nav>
      <main>
        <Outlet />
      </main>
    </div>
  );
}

export function SeverityBadge({ severity }: { severity: string }) {
  return <span className={`badge ${severity}`}>{severity}</span>;
}

export function Disclaimer() {
  return (
    <div className="banner">
      Scores measure unusual volume and price behavior. They are not a probability, a forecast, or a buy/sell
      recommendation.
    </div>
  );
}

export function formatTime(value: string) {
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return value;
  return date.toLocaleString(undefined, { hour12: false });
}

export function formatNumber(value: number | null | undefined, digits = 2) {
  if (value === null || value === undefined) return "—";
  return value.toLocaleString(undefined, { maximumFractionDigits: digits });
}
