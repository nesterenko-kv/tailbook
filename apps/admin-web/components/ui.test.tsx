import { describe, it, expect, vi } from "vitest";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { Badge, ErrorBanner, SuccessBanner, PageHeader, Card, EmptyState, LoadingState, Input, PrimaryButton, SecondaryButton } from "./ui";

vi.mock("next/link", () => ({ default: ({ children, href }: { children: React.ReactNode; href: string }) => <a href={href}>{children}</a> }));

describe("Badge", () => {
    it("renders children text", () => {
        render(<Badge>Active</Badge>);
        expect(screen.getByText("Active")).toBeInTheDocument();
    });

    it("applies default tone classes", () => {
        const { container } = render(<Badge>Default</Badge>);
        expect(container.firstChild).toHaveClass("border-slate-700");
    });

    it("applies success tone", () => {
        const { container } = render(<Badge tone="success">Done</Badge>);
        expect(container.firstChild).toHaveClass("text-emerald-300");
    });

    it("applies warning tone", () => {
        const { container } = render(<Badge tone="warning">Pending</Badge>);
        expect(container.firstChild).toHaveClass("text-amber-300");
    });

    it("applies danger tone", () => {
        const { container } = render(<Badge tone="danger">Error</Badge>);
        expect(container.firstChild).toHaveClass("text-rose-300");
    });
});

describe("ErrorBanner", () => {
    it("renders the error message", () => {
        render(<ErrorBanner message="Something went wrong" />);
        expect(screen.getByText("Something went wrong")).toBeInTheDocument();
    });

    it("returns null when message is null", () => {
        const { container } = render(<ErrorBanner message={null} />);
        expect(container.firstChild).toBeNull();
    });

    it("returns null when message is undefined", () => {
        const { container } = render(<ErrorBanner />);
        expect(container.firstChild).toBeNull();
    });

    it("returns null when message is empty string", () => {
        const { container } = render(<ErrorBanner message="" />);
        expect(container.firstChild).toBeNull();
    });
});

describe("SuccessBanner", () => {
    it("renders the success message", () => {
        render(<SuccessBanner message="Saved successfully" />);
        expect(screen.getByText("Saved successfully")).toBeInTheDocument();
    });

    it("returns null when message is null", () => {
        const { container } = render(<SuccessBanner message={null} />);
        expect(container.firstChild).toBeNull();
    });
});

describe("PageHeader", () => {
    it("renders title", () => {
        render(<PageHeader title="Dashboard" />);
        expect(screen.getByText("Dashboard")).toBeInTheDocument();
    });

    it("renders eyebrow when provided", () => {
        render(<PageHeader eyebrow="Overview" title="Dashboard" />);
        expect(screen.getByText("Overview")).toBeInTheDocument();
    });

    it("does not render eyebrow when not provided", () => {
        render(<PageHeader title="Dashboard" />);
        expect(screen.queryByText("Overview")).not.toBeInTheDocument();
    });

    it("renders description when provided", () => {
        render(<PageHeader title="Dashboard" description="Manage your dashboard settings" />);
        expect(screen.getByText("Manage your dashboard settings")).toBeInTheDocument();
    });

    it("renders action element when provided", () => {
        render(<PageHeader title="Dashboard" action={<button>Add</button>} />);
        expect(screen.getByRole("button", { name: "Add" })).toBeInTheDocument();
    });

    it("does not render action when not provided", () => {
        const { container } = render(<PageHeader title="Dashboard" />);
        const buttons = container.querySelectorAll("button");
        expect(buttons).toHaveLength(0);
    });
});

describe("Card", () => {
    it("renders title and children", () => {
        render(<Card title="Settings"><p>Content</p></Card>);
        expect(screen.getByText("Settings")).toBeInTheDocument();
        expect(screen.getByText("Content")).toBeInTheDocument();
    });

    it("renders description when provided", () => {
        render(<Card title="Settings" description="Configure your preferences"><p>Content</p></Card>);
        expect(screen.getByText("Configure your preferences")).toBeInTheDocument();
    });
});

describe("EmptyState", () => {
    it("renders title", () => {
        render(<EmptyState title="No items found" />);
        expect(screen.getByText("No items found")).toBeInTheDocument();
    });

    it("renders description when provided", () => {
        render(<EmptyState title="No items" description="Try adjusting your filters" />);
        expect(screen.getByText("Try adjusting your filters")).toBeInTheDocument();
    });
});

describe("LoadingState", () => {
    it("renders default label", () => {
        render(<LoadingState />);
        expect(screen.getByText("Loading...")).toBeInTheDocument();
    });

    it("renders custom label", () => {
        render(<LoadingState label="Fetching data…" />);
        expect(screen.getByText("Fetching data…")).toBeInTheDocument();
    });

    it("has aria-live polite", () => {
        render(<LoadingState />);
        expect(screen.getByText("Loading...")).toHaveAttribute("aria-live", "polite");
    });
});

describe("Input", () => {
    it("renders input with forwarded props", () => {
        render(<Input placeholder="Enter name" data-testid="name-input" />);
        const input = screen.getByTestId("name-input");
        expect(input).toBeInTheDocument();
        expect(input).toHaveAttribute("placeholder", "Enter name");
    });

    it("accepts user input", async () => {
        const user = userEvent.setup();
        render(<Input data-testid="name-input" />);
        const input = screen.getByTestId("name-input");
        await user.type(input, "John");
        expect(input).toHaveValue("John");
    });
});

describe("PrimaryButton", () => {
    it("renders button with text", () => {
        render(<PrimaryButton>Save</PrimaryButton>);
        expect(screen.getByRole("button", { name: "Save" })).toBeInTheDocument();
    });

    it("can be disabled", () => {
        render(<PrimaryButton disabled>Save</PrimaryButton>);
        expect(screen.getByRole("button", { name: "Save" })).toBeDisabled();
    });

    it("fires onClick handler", async () => {
        const user = userEvent.setup();
        const onClick = vi.fn();
        render(<PrimaryButton onClick={onClick}>Click</PrimaryButton>);
        await user.click(screen.getByRole("button", { name: "Click" }));
        expect(onClick).toHaveBeenCalledOnce();
    });
});

describe("SecondaryButton", () => {
    it("renders button with text", () => {
        render(<SecondaryButton>Cancel</SecondaryButton>);
        expect(screen.getByRole("button", { name: "Cancel" })).toBeInTheDocument();
    });
});
