import { Navigate, Route, Routes } from 'react-router-dom';
import { useAuth } from './auth/AuthProvider.tsx';
import { PageLayout } from './components/templates/PageLayout.tsx';
import { LoginPage } from './pages/LoginPage.tsx';
import { MyCommissionPage } from './pages/MyCommissionPage.tsx';
import { RulesPage } from './pages/RulesPage.tsx';
import { PeriodOverviewPage } from './pages/PeriodOverviewPage.tsx';
import { UsersPage } from './pages/UsersPage.tsx';
import { ImportPage } from './pages/ImportPage.tsx';
import { ImportLogPage } from './pages/ImportLogPage.tsx';

export default function App() {
  const { isAuthenticated, user } = useAuth();

  if (!isAuthenticated) return <LoginPage />;

  const home = user?.employeeId != null ? '/commission' : '/periods';

  return (
    <PageLayout>
      <Routes>
        <Route path="/" element={<Navigate to={home} replace />} />
        <Route path="/commission" element={<MyCommissionPage />} />
        <Route path="/rules" element={<RulesPage />} />
        <Route path="/periods" element={<PeriodOverviewPage />} />
        <Route path="/users" element={<UsersPage />} />
        <Route path="/import" element={<ImportPage />} />
        <Route path="/import-log" element={<ImportLogPage />} />
        <Route path="*" element={<Navigate to={home} replace />} />
      </Routes>
    </PageLayout>
  );
}
