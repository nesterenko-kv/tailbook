import type { ReactNode, SVGProps } from "react";

type IconProps = SVGProps<SVGSVGElement>;

function BaseIcon({ children, className, viewBox = "0 0 24 24", ...props }: IconProps & { children: ReactNode; viewBox?: string }) {
  return (
    <svg viewBox={viewBox} fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round" className={className} {...props}>
      {children}
    </svg>
  );
}

export function HeartIcon(props: IconProps) { return <BaseIcon {...props}><path d="M12 20.5s-7-4.35-9-8.86C1.5 8.17 3.55 4.5 7.6 4.5c2.08 0 3.49 1.09 4.4 2.43.91-1.34 2.32-2.43 4.4-2.43 4.05 0 6.1 3.67 4.6 7.14-2 4.51-9 8.86-9 8.86Z" /></BaseIcon>; }
export function ArrowRightIcon(props: IconProps) { return <BaseIcon {...props}><path d="M5 12h14" /><path d="m13 6 6 6-6 6" /></BaseIcon>; }
export function ArrowLeftIcon(props: IconProps) { return <BaseIcon {...props}><path d="M19 12H5" /><path d="m11 18-6-6 6-6" /></BaseIcon>; }
export function StarIcon(props: IconProps) { return <BaseIcon {...props}><path d="m12 3 2.8 5.68 6.27.91-4.54 4.42 1.07 6.24L12 17.3l-5.6 2.95 1.07-6.24L2.93 9.6l6.27-.91L12 3Z" /></BaseIcon>; }
export function ClockIcon(props: IconProps) { return <BaseIcon {...props}><circle cx="12" cy="12" r="9" /><path d="M12 7.5v5l3.5 2" /></BaseIcon>; }
export function ShieldIcon(props: IconProps) { return <BaseIcon {...props}><path d="M12 3l7 3.2v5.3c0 4.6-3 7.74-7 9.5-4-1.76-7-4.9-7-9.5V6.2L12 3Z" /></BaseIcon>; }
export function PhoneIcon(props: IconProps) { return <BaseIcon {...props}><path d="M22 16.5v2a2 2 0 0 1-2.18 2 19.86 19.86 0 0 1-8.65-3.08A19.42 19.42 0 0 1 4.58 10.8 19.86 19.86 0 0 1 1.5 2.15 2 2 0 0 1 3.49 0h2A2 2 0 0 1 7.47 1.7l.64 3.53a2 2 0 0 1-.56 1.77L6.18 8.37a16 16 0 0 0 9.45 9.45l1.37-1.37a2 2 0 0 1 1.77-.56l3.53.64A2 2 0 0 1 22 16.5Z" /></BaseIcon>; }
export function MapPinIcon(props: IconProps) { return <BaseIcon {...props}><path d="M12 21s6-5.33 6-11a6 6 0 1 0-12 0c0 5.67 6 11 6 11Z" /><circle cx="12" cy="10" r="2.5" /></BaseIcon>; }
export function InstagramIcon(props: IconProps) { return <BaseIcon {...props}><rect x="3.5" y="3.5" width="17" height="17" rx="4" /><circle cx="12" cy="12" r="4" /><circle cx="17.2" cy="6.8" r=".8" fill="currentColor" stroke="none" /></BaseIcon>; }
export function CalendarIcon(props: IconProps) { return <BaseIcon {...props}><rect x="3" y="5" width="18" height="16" rx="2" /><path d="M16 3v4" /><path d="M8 3v4" /><path d="M3 10h18" /></BaseIcon>; }
export function CheckCircleIcon(props: IconProps) { return <BaseIcon {...props}><circle cx="12" cy="12" r="9" /><path d="m8.5 12 2.3 2.3 4.7-4.8" /></BaseIcon>; }
export function HomeIcon(props: IconProps) { return <BaseIcon {...props}><path d="M3 11.5 12 4l9 7.5" /><path d="M6.5 10.5V20h11v-9.5" /></BaseIcon>; }
export function PlusIcon(props: IconProps) { return <BaseIcon {...props}><path d="M12 5v14" /><path d="M5 12h14" /></BaseIcon>; }
export function PawIcon(props: IconProps) { return <BaseIcon {...props}><ellipse cx="12" cy="14" rx="3.8" ry="3.1" /><circle cx="7.2" cy="9" r="1.7" /><circle cx="10.2" cy="6.8" r="1.7" /><circle cx="13.8" cy="6.8" r="1.7" /><circle cx="16.8" cy="9" r="1.7" /></BaseIcon>; }
export function UserIcon(props: IconProps) { return <BaseIcon {...props}><circle cx="12" cy="8" r="3.5" /><path d="M5 20c1.7-3.1 4.1-4.5 7-4.5S17.3 16.9 19 20" /></BaseIcon>; }
export function RefreshIcon(props: IconProps) { return <BaseIcon {...props}><path d="M20 11a8 8 0 1 0-2.34 5.66" /><path d="M20 4v7h-7" /></BaseIcon>; }
export function CloseIcon(props: IconProps) { return <BaseIcon {...props}><path d="m6 6 12 12" /><path d="m18 6-12 12" /></BaseIcon>; }
export function UploadIcon(props: IconProps) { return <BaseIcon {...props}><path d="M12 16V5" /><path d="m8 9 4-4 4 4" /><path d="M4 19h16" /></BaseIcon>; }
export function SparklesIcon(props: IconProps) { return <BaseIcon {...props}><path d="m12 3 1.5 4.5L18 9l-4.5 1.5L12 15l-1.5-4.5L6 9l4.5-1.5L12 3Z" /><path d="m19 15 .9 2.1L22 18l-2.1.9L19 21l-.9-2.1L16 18l2.1-.9L19 15Z" /><path d="m5 14 .7 1.6L7.3 16l-1.6.7L5 18.3l-.7-1.6L2.7 16l1.6-.4L5 14Z" /></BaseIcon>; }
export function CheckIcon(props: IconProps) { return <BaseIcon {...props}><path d="m5 12 4 4L19 7" /></BaseIcon>; }
export function ChevronDownIcon(props: IconProps) { return <BaseIcon {...props}><path d="m6 9 6 6 6-6" /></BaseIcon>; }
