import { StatsPanel } from "./components/StatsPanel";
import { RateLimitConfigTable } from "./components/RateLimitConfigTable";
import "./App.css";

function App() {
  return (
    <div className="app">
      <h1>API Rate Limiting Gateway — Dashboard</h1>
      <StatsPanel />
      <hr />
      <RateLimitConfigTable />
    </div>
  );
}

export default App;
