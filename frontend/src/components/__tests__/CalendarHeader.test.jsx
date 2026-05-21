import { describe, test } from "vitest";
import CalendarHeader from "../CalendarHeader";
import { render, screen } from "@testing-library/react";
import { vi } from "vitest";
import Context from "../../context/Context";
import dayjs from "dayjs";

describe("Calendar Header", () => {
  test("Shows current date on first page load", () => {
    render(
      <Context.Provider
        value={{
          selectedDate: dayjs(),
          isMonthView: false,
          isWeekView: true,
          isDayView: false,
        }}
      >
        <CalendarHeader
          showChat={false}
          onOpenAIChat={vi.fn()}
          showSidebar={false}
          onOpenSidebar={vi.fn()}
        />
        ,
      </Context.Provider>,
    );

    const date = new Date();
    const month = date.toLocaleString("en-US", {
      month: "long",
    });

    const year = date.getFullYear();

    const formatted = `${month}, ${year}`;

    expect(screen.getByText(formatted)).toBeInTheDocument();
  });
});
