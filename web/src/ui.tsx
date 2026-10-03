import type { ReactNode } from "react";
import { NavLink, Outlet } from "react-router-dom";

const links = [
  { to: "/", label: "Dashboard", end: true },
  { to: "/watchlists", label: "Watchlists" },
  { to: "/rules", label: "Detection rules" },
  { to: "/health", label: "System health" },
  { to: "/backtests", label: "Replay / backtest" },
  { to: "/alerts", label: "Alerts" }
] as const;

export function Layout() {
  return (
    <div className="shell">
      <nav className="side">
        <div className="brand">
          <span className="mark" aria-hidden="true" />
          <div>
            <h1>MarketPulse</h1>
            <p>Anomaly scanner</p>
          </div>
        </div>
        <div className="nav-links">
          {links.map((link) => (
            <NavLink
              key={link.to}
              to={link.to}
              end={"end" in link ? link.end : false}
              className={({ isActive }) => (isActive ? "active" : "")}
            >
              {link.label}
            </NavLink>
          ))}
        </div>
        <p className="nav-note">Engineering metrics only. Not a trading product.</p>
      </nav>
      <main>
        <Outlet />
      </main>
    </div>
  );
}

export function PageHeader({
  kicker,
  title,
  children
}: {
  kicker?: string;
  title: string;
  children?: ReactNode;
}) {
  return (
    <header className="page-head">
      {kicker ? <p className="kicker">{kicker}</p> : null}
      <h2>{title}</h2>
      {children}
    </header>
  );
}

export function SeverityBadge({ severity }: { severity: string }) {
  return <span className={`badge ${severity}`}>{severity}</span>;
}

export function ScoreMeter({ score, severity }: { score: number; severity?: string }) {
  const width = Math.min(100, Math.max(0, score));
  return (
    <div className="score-meter">
      <span className="score-num">{formatNumber(score, 1)}</span>
      <div className="score-track" aria-hidden="true">
        <div className={`score-fill ${severity ?? ""}`} style={{ width: `${width}%` }} />
      </div>
    </div>
  );
}

export function EmptyState({ children }: { children: ReactNode }) {
  return <div className="empty">{children}</div>;
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
