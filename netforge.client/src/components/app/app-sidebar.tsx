import { Link } from 'react-router';

import { ShellBrand } from '@/components/app/shell-brand';
import { SidebarNav } from '@/components/app/nav';
import { Sidebar, SidebarContent, SidebarHeader, SidebarRail } from '@/components/ui/sidebar';

// Authenticated app navigation. Built on the shadcn <Sidebar>: collapses to an icon rail on desktop
// (toggle in the topbar, state persisted via cookie) and becomes an off-canvas drawer on mobile. The
// brand swaps to its mark-only lockup when the rail is collapsed to icons. The default "left" side is
// logical (the Sidebar uses inline-start insets), so it mirrors to the right under RTL automatically.
export function AppSidebar() {
  return (
    <Sidebar collapsible="icon" data-tour="nav">
      <SidebarHeader className="h-14 justify-center">
        <Link
          to="/"
          className="focus-visible:ring-ring/50 flex items-center rounded-md px-1 outline-none focus-visible:ring-[3px]"
        >
          <ShellBrand className="group-data-[collapsible=icon]:hidden" />
          <ShellBrand markOnly className="hidden group-data-[collapsible=icon]:inline-flex" />
        </Link>
      </SidebarHeader>
      <SidebarContent>
        <SidebarNav />
      </SidebarContent>
      <SidebarRail />
    </Sidebar>
  );
}
