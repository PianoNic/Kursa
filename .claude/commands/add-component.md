Create a new Angular component/feature for the frontend. Follow the established pattern:

1. Determine if this is a feature component, shared component, or layout component
2. Generate the standalone component using `ng g c` with the correct path
3. Use spartan.ng components (brn-* for behavior, hlm-* for styled) where applicable
4. Use Tailwind CSS for styling — no custom CSS files unless absolutely necessary
5. Use Angular signals for local state management
6. Use the new control flow syntax (@if, @for, @switch)
7. Create a service if the component needs to fetch data
8. Add the route if it's a routable feature (lazy-loaded)
9. Run `ng build` to verify compilation
10. Ensure dark mode works (Tailwind `dark:` variants)

Component to create: $ARGUMENTS
