// Sidebar Menu Management
class SidebarManager {
    constructor() {
        this.sidebar = document.getElementById('sidebar');
        this.sidebarToggle = document.getElementById('sidebarToggle');
        this.topBarToggle = document.getElementById('topBarToggle');
        this.sidebarOverlay = document.getElementById('sidebarOverlay');
        this.mainContent = document.getElementById('mainContent');
        this.isCollapsed = localStorage.getItem('sidebarCollapsed') === 'true';
        this.isMobile = window.innerWidth <= 768;

        this.init();
    }

    init() {
        // Initialize sidebar state
        if (this.isCollapsed && !this.isMobile) {
            this.collapse();
        }

        // Setup event listeners
        if (this.sidebarToggle) {
            this.sidebarToggle.addEventListener('click', () => this.toggle());
        }

        if (this.topBarToggle) {
            this.topBarToggle.addEventListener('click', () => this.toggle());
        }

        if (this.sidebarOverlay) {
            this.sidebarOverlay.addEventListener('click', () => this.hide());
        }

        // Setup submenu toggles
        this.setupSubmenus();

        // Handle window resize
        window.addEventListener('resize', () => {
            const wasMobile = this.isMobile;
            this.isMobile = window.innerWidth <= 768;

            if (wasMobile !== this.isMobile) {
                if (this.isMobile) {
                    this.hide();
                } else {
                    if (!this.isCollapsed) {
                        this.show();
                    }
                }
            }
        });

        // Set active menu item based on current page
        this.setActiveMenuItem();
    }

    toggle() {
        if (this.isMobile) {
            this.sidebar?.classList.toggle('show');
            this.sidebarOverlay?.classList.toggle('show');
        } else {
            if (this.sidebar?.classList.contains('collapsed')) {
                this.expand();
            } else {
                this.collapse();
            }
        }
    }

    collapse() {
        this.sidebar?.classList.add('collapsed');
        this.mainContent?.classList.add('sidebar-collapsed');
        this.isCollapsed = true;
        localStorage.setItem('sidebarCollapsed', 'true');
    }

    expand() {
        this.sidebar?.classList.remove('collapsed');
        this.mainContent?.classList.remove('sidebar-collapsed');
        this.isCollapsed = false;
        localStorage.setItem('sidebarCollapsed', 'false');
    }

    show() {
        if (this.isMobile) {
            this.sidebar?.classList.add('show');
            this.sidebarOverlay?.classList.add('show');
        }
    }

    hide() {
        if (this.isMobile) {
            this.sidebar?.classList.remove('show');
            this.sidebarOverlay?.classList.remove('show');
        }
    }

    setupSubmenus() {
        const menuItems = document.querySelectorAll('.menu-item.has-submenu');
        
        menuItems.forEach(item => {
            const menuLink = item.querySelector('.menu-link');
            if (menuLink) {
                menuLink.addEventListener('click', (e) => {
                    e.preventDefault();
                    const isExpanded = item.classList.contains('expanded');
                    
                    // Close other submenus in the same group (optional)
                    // const group = item.closest('.menu-group');
                    // group?.querySelectorAll('.menu-item.expanded').forEach(expandedItem => {
                    //     if (expandedItem !== item) {
                    //         expandedItem.classList.remove('expanded');
                    //     }
                    // });
                    
                    if (isExpanded) {
                        item.classList.remove('expanded');
                    } else {
                        item.classList.add('expanded');
                    }
                });
            }
        });
    }

    setActiveMenuItem() {
        const currentPath = window.location.pathname.toLowerCase();
        
        // Remove all active classes
        document.querySelectorAll('.menu-item > a.active, .submenu-item > a.active').forEach(link => {
            link.classList.remove('active');
        });

        // Find and activate matching menu item
        document.querySelectorAll('.menu-item > a, .submenu-item > a').forEach(link => {
            const href = link.getAttribute('href')?.toLowerCase();
            if (href && currentPath.includes(href.replace(/^\//, '').split('/')[0])) {
                link.classList.add('active');
                
                // Expand parent submenu if it's a submenu item
                const submenuItem = link.closest('.submenu-item');
                if (submenuItem) {
                    const parentMenuItem = submenuItem.closest('.menu-item.has-submenu');
                    if (parentMenuItem) {
                        parentMenuItem.classList.add('expanded');
                    }
                }
            }
        });
    }
}

// Initialize sidebar when DOM is ready
document.addEventListener('DOMContentLoaded', function() {
    new SidebarManager();
});
