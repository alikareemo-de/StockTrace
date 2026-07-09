import { useEffect, useState } from 'react';
import { getStoredUser, loadMasterData, logout } from './api/services';
import { InventoryPage } from './pages/InventoryPage';
import { LoginPage } from './pages/LoginPage';
import { MasterDataPage } from './pages/MasterDataPage';
import { PurchasesPage } from './pages/PurchasesPage';
import { RealtimePage } from './pages/RealtimePage';
import { ReportsPage } from './pages/ReportsPage';
import { SalesPage } from './pages/SalesPage';
import { TransfersPage } from './pages/TransfersPage';
import { appConfig } from './config';
import type { AuthUser, MasterData } from './models';
import { Permissions } from './models';

type PageKey = 'dashboard' | 'master' | 'inventory' | 'purchases' | 'sales' | 'transfers' | 'reports' | 'realtime';

const pages: Array<{ key: PageKey; label: string }> = [
  { key: 'dashboard', label: 'Dashboard' },
  { key: 'master', label: 'Master Data' },
  { key: 'inventory', label: 'Inventory' },
  { key: 'purchases', label: 'Purchases' },
  { key: 'sales', label: 'Sales' },
  { key: 'transfers', label: 'Transfers' },
  { key: 'reports', label: 'Reports' },
  { key: 'realtime', label: 'Realtime' }
];

export function App() {
  const [activePage, setActivePage] = useState<PageKey>('dashboard');
  const [user, setUser] = useState<AuthUser | null>(() => getStoredUser());
  const [data, setData] = useState<MasterData | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  async function reload() {
    setLoading(true);
    try {
      setData(await loadMasterData());
      setError(null);
    } catch (loadError) {
      setError(loadError instanceof Error ? loadError.message : 'Failed to load master data.');
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    if (user) void reload();
  }, [user]);

  useEffect(() => {
    function handleUnauthorized() {
      logout();
      setUser(null);
      setData(null);
      setActivePage('dashboard');
    }

    window.addEventListener('stocktrace:unauthorized', handleUnauthorized);
    return () => window.removeEventListener('stocktrace:unauthorized', handleUnauthorized);
  }, []);

  if (!user) {
    return <LoginPage onLogin={setUser} />;
  }

  const visiblePages = pages.filter((page) => page.key === 'dashboard' || hasPagePermission(page.key, user));

  return (
    <div className="app-shell">
      <aside className="sidebar">
        <div>
          <h1>StockTrace Testing UI</h1>
          <p>{user.displayName}</p>
          <p>{user.role}</p>
        </div>
        <nav>
          {visiblePages.map((page) => (
            <button className={activePage === page.key ? 'active' : ''} onClick={() => setActivePage(page.key)} key={page.key}>
              {page.label}
            </button>
          ))}
          <button
            onClick={() => {
              logout();
              setUser(null);
              setData(null);
              setActivePage('dashboard');
            }}
          >
            Logout
          </button>
        </nav>
      </aside>
      <main>
        {loading && <div className="panel">Loading master data from backend...</div>}
        {error && (
          <div className="panel error-panel">
            <h2>Backend connection failed</h2>
            <p>{error}</p>
            <p>Make sure the backend is running on {appConfig.apiBaseUrl || 'the configured API URL'}, then reload master data.</p>
            <button onClick={reload}>Reload</button>
          </div>
        )}
        {!loading && !error && data && renderPage(activePage, data, reload, setActivePage, user)}
      </main>
    </div>
  );
}

function renderPage(
  page: PageKey,
  data: MasterData,
  reload: () => Promise<void>,
  navigate: (page: PageKey) => void,
  user: AuthUser
) {
  if (!hasPagePermission(page, user) && page !== 'dashboard') return <AccessDenied />;
  if (page === 'master') return <MasterDataPage data={data} reload={reload} permissions={user.permissions} />;
  if (page === 'inventory') return <InventoryPage data={data} />;
  if (page === 'purchases') return <PurchasesPage data={data} permissions={user.permissions} />;
  if (page === 'sales') return <SalesPage data={data} permissions={user.permissions} />;
  if (page === 'transfers') return <TransfersPage data={data} permissions={user.permissions} />;
  if (page === 'reports') return <ReportsPage data={data} permissions={user.permissions} />;
  if (page === 'realtime') return <RealtimePage accessToken={user.accessToken} />;

  return (
    <section className="section">
      <div className="section-heading">
        <h2>Dashboard</h2>
        <p>Use this tool to exercise every API endpoint from the inventory module.</p>
      </div>
      <div className="dashboard-grid">
        {pages
          .filter((item): item is { key: Exclude<PageKey, 'dashboard'>; label: string } =>
            item.key !== 'dashboard' && hasPagePermission(item.key, user)
          )
          .map((item) => (
            <button className="dashboard-card" onClick={() => navigate(item.key)} key={item.key}>
              <strong>{item.label}</strong>
              <span>{dashboardText[item.key]}</span>
            </button>
          ))}
      </div>
    </section>
  );
}

function hasPagePermission(page: PageKey, user: AuthUser): boolean {
  const permissions = user.permissions;
  if (page === 'dashboard') return true;
  if (page === 'master') return permissions.includes(Permissions.MasterDataRead);
  if (page === 'inventory') return permissions.includes(Permissions.InventoryRead);
  if (page === 'purchases') return permissions.includes(Permissions.PurchaseReceiptsRead) || permissions.includes(Permissions.PurchaseReceiptsCreate);
  if (page === 'sales') return permissions.includes(Permissions.SalesRead) || permissions.includes(Permissions.SalesCreate);
  if (page === 'transfers') return permissions.includes(Permissions.StockTransfersRead) || permissions.includes(Permissions.StockTransfersCreate);
  if (page === 'reports') return permissions.includes(Permissions.ReportsRead);
  if (page === 'realtime') return permissions.includes(Permissions.RealtimeRead);
  return false;
}

function AccessDenied() {
  return (
    <section className="panel error-panel">
      <h2>Access denied</h2>
      <p>Your current user does not have permission to open this page.</p>
    </section>
  );
}

const dashboardText: Record<Exclude<PageKey, 'dashboard'>, string> = {
  master: 'Read master data and update low-stock thresholds.',
  inventory: 'Check current availability by warehouse and product.',
  purchases: 'Create and read purchase receipts.',
  sales: 'Create and read sales with FIFO allocations.',
  transfers: 'Create and read stock transfers.',
  reports: 'Run inventory movement reports.',
  realtime: 'Watch SignalR stock-change and low-stock events.'
};
