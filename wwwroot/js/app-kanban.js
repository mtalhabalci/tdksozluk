"use strict";!async function(){const o=document.querySelector(".kanban-update-item-sidebar"),e=document.querySelector(".kanban-wrapper"),t=document.querySelector(".comment-editor"),r=document.querySelector(".kanban-add-new-board"),d=[].slice.call(document.querySelectorAll(".kanban-add-board-input")),a=document.querySelector(".kanban-add-board-btn"),n=document.querySelector("#due-date"),i=$(".select2"),s=document.querySelector("html").getAttribute("data-assets-path"),l=new bootstrap.Offcanvas(o);var c=await fetch(s+"json/kanban.json");function m(e){return e.id?"<div class='badge "+$(e.element).data("color")+" rounded-pill'> "+e.text+"</div>":e.text}c.ok||console.error("error",c),c=await c.json(),n&&n.flatpickr({monthSelectorType:"static",static:!0,altInput:!0,altFormat:"j F, Y",dateFormat:"Y-m-d"}),i.length&&i.each(function(){var e=$(this);select2Focus(e),e.wrap().select2({placeholder:"Select Label",dropdownParent:e.parent(),templateResult:m,templateSelection:m,escapeMarkup:function(e){return e}})}),t&&new Quill(t,{modules:{toolbar:".comment-toolbar"},placeholder:"Write a Comment...",theme:"snow"});const b=()=>`
  <div class="dropdown">
      <i class="dropdown-toggle icon-base ri ri-more-2-fill cursor-pointer"
         id="board-dropdown"
         data-bs-toggle="dropdown"
         aria-haspopup="true"
         aria-expanded="false">
      </i>
      <div class="dropdown-menu dropdown-menu-end" aria-labelledby="board-dropdown">
          <a class="dropdown-item delete-board" href="javascript:void(0)">
              <i class="icon-base ri ri-delete-bin-7-line icon-sm me-1"></i>
              <span class="align-middle">Delete</span>
          </a>
          <a class="dropdown-item" href="javascript:void(0)">
              <i class="icon-base ri ri-edit-2-line icon-sm me-1"></i>
              <span class="align-middle">Rename</span>
          </a>
          <a class="dropdown-item" href="javascript:void(0)">
              <i class="icon-base ri ri-archive-line icon-sm me-1"></i>
              <span class="align-middle">Archive</span>
          </a>
      </div>
  </div>
`,u=()=>`
<div class="dropdown kanban-tasks-item-dropdown">
    <i class="dropdown-toggle icon-base ri ri-more-2-fill icon-sm"
       id="kanban-tasks-item-dropdown"
       data-bs-toggle="dropdown"
       aria-haspopup="true"
       aria-expanded="false">
    </i>
    <div class="dropdown-menu dropdown-menu-end" aria-labelledby="kanban-tasks-item-dropdown">
        <a class="dropdown-item" href="javascript:void(0)">Copy task link</a>
        <a class="dropdown-item" href="javascript:void(0)">Duplicate task</a>
        <a class="dropdown-item delete-task" href="javascript:void(0)">Delete</a>
    </div>
</div>
`,p=(e="",t=!1,a="",n="",r="")=>{const d=t?" pull-up":"",o=a?"avatar-"+a:"",i=r?r.split(","):[];return e?e.split(",").map((e,t,a)=>{a=n&&t!==a.length-1?" me-"+n:"",t=i[t]||"";return`
            <div class="avatar ${o}${a} w-px-26 h-px-26"
                 data-bs-toggle="tooltip"
                 data-bs-placement="top"
                 title="${t}">
                <img src="${s}img/avatars/${e}"
                     alt="Avatar"
                     class="rounded-circle${d}">
            </div>
        `}).join(""):""},g=new jKanban({element:".kanban-wrapper",gutter:"12px",widthBoard:"250px",dragItems:!0,boards:c,dragBoards:!0,addItemButton:!0,buttonContent:"+ Add Item",itemAddOptions:{enabled:!0,content:"+ Add New Item",class:"kanban-title-button btn btn-default",footer:!1},click:e=>{var t=e,a=(t.getAttribute("data-eid")?t.querySelector(".kanban-text"):t).textContent,n=t.getAttribute("data-due-date"),r=new Date,d=r.getFullYear(),n=n?n+", "+d:`${r.getDate()} ${r.toLocaleString("en",{month:"long"})}, `+d,r=t.getAttribute("data-badge-text"),d=t.getAttribute("data-assigned");l.show(),o.querySelector("#title").value=a,o.querySelector("#due-date").nextSibling.value=n,$(".kanban-update-item-sidebar").find(i).val(r).trigger("change"),o.querySelector(".assigned").innerHTML="",o.querySelector(".assigned").insertAdjacentHTML("afterbegin",p(d,!1,"sm","2",e.getAttribute("data-members"))+`
        <div class="avatar avatar-sm ms-2">
            <span class="avatar-initial rounded-circle bg-label-secondary">
                <i class="icon-base ri ri-add-line"></i>
            </span>
        </div>`)},buttonClick:(e,a)=>{const n=document.createElement("form");n.setAttribute("class","new-item-form"),n.innerHTML=`
        <div class="mb-4">
            <textarea class="form-control add-new-item" rows="2" placeholder="Add Content" autofocus required></textarea>
        </div>
        <div class="mb-4">
            <button type="submit" class="btn btn-primary btn-sm me-3">Add</button>
            <button type="button" class="btn btn-label-secondary btn-sm cancel-add-item">Cancel</button>
        </div>
      `,g.addForm(a,n),n.addEventListener("submit",e=>{e.preventDefault();var t=Array.from(document.querySelectorAll(`.kanban-board[data-id="${a}"] .kanban-item`));g.addElement(a,{title:`<span class="kanban-text">${e.target[0].value}</span>`,id:a+"-"+(t.length+1)}),Array.from(document.querySelectorAll(`.kanban-board[data-id="${a}"] .kanban-text`)).forEach(e=>{e.insertAdjacentHTML("beforebegin",u())}),Array.from(document.querySelectorAll(".kanban-item .kanban-tasks-item-dropdown")).forEach(e=>{e.addEventListener("click",e=>e.stopPropagation())}),Array.from(document.querySelectorAll(`.kanban-board[data-id="${a}"] .delete-task`)).forEach(t=>{t.addEventListener("click",()=>{var e=t.closest(".kanban-item").getAttribute("data-eid");g.removeElement(e)})}),n.remove()}),n.querySelector(".cancel-add-item").addEventListener("click",()=>n.remove())}}),v=(e&&new PerfectScrollbar(e),document.querySelector(".kanban-container"));var c=Array.from(document.querySelectorAll(".kanban-title-board")),f=Array.from(document.querySelectorAll(".kanban-item"));f.length&&f.forEach(e=>{var t,a,n=`<span class="kanban-text">${e.textContent}</span>`;let r="";e.getAttribute("data-image")&&(r=`
              <img class="img-fluid rounded-4 mb-2"
                   src="${s}img/elements/${e.getAttribute("data-image")}">
          `),e.textContent="",e.getAttribute("data-badge")&&e.getAttribute("data-badge-text")&&e.insertAdjacentHTML("afterbegin",(t=e.getAttribute("data-badge"),a=e.getAttribute("data-badge-text"),`
<div class="d-flex justify-content-between flex-wrap align-items-center mb-2">
    <div class="item-badges">
        <div class="badge rounded-pill bg-label-${t}">${a}</div>
    </div>
    ${u()}
</div>
`+r+n)),(e.getAttribute("data-comments")||e.getAttribute("data-due-date")||e.getAttribute("data-assigned"))&&e.insertAdjacentHTML("beforeend",(t=e.getAttribute("data-attachments")||0,a=e.getAttribute("data-comments")||0,n=e.getAttribute("data-assigned")||"",e=e.getAttribute("data-members")||"",`
<div class="d-flex justify-content-between align-items-center flex-wrap mt-2">
    <div class="d-flex">
        <span class="d-flex align-items-center me-4">
            <i class="icon-base ri ri-attachment-2 me-1"></i>
            <span class="attachments">${t}</span>
        </span>
        <span class="d-flex align-items-center">
            <i class="icon-base ri ri-wechat-line me-1"></i>
            <span>${a}</span>
        </span>
    </div>
    <div class="avatar-group d-flex align-items-center assigned-avatar">
        ${p(n,!0,"xs",null,e)}
    </div>
</div>
`))});Array.from(document.querySelectorAll('[data-bs-toggle="tooltip"]')).forEach(e=>{new bootstrap.Tooltip(e)});f=Array.from(document.querySelectorAll(".kanban-tasks-item-dropdown"));f.length&&f.forEach(e=>{e.addEventListener("click",e=>{e.stopPropagation()})}),a&&a.addEventListener("click",()=>{d.forEach(e=>{e.value="",e.classList.toggle("d-none")})}),v&&v.append(r),c&&c.forEach(e=>{e.addEventListener("mouseenter",()=>{e.contentEditable="true"}),e.insertAdjacentHTML("afterend",b())}),Array.from(document.querySelectorAll(".delete-board")).forEach(t=>{t.addEventListener("click",()=>{var e=t.closest(".kanban-board").getAttribute("data-id");g.removeBoard(e)})});Array.from(document.querySelectorAll(".delete-task")).forEach(t=>{t.addEventListener("click",()=>{var e=t.closest(".kanban-item").getAttribute("data-eid");g.removeElement(e)})});f=document.querySelector(".kanban-add-board-cancel-btn");f&&f.addEventListener("click",()=>{d.forEach(e=>{e.classList.toggle("d-none")})}),r&&r.addEventListener("submit",e=>{e.preventDefault();var e=e.target.querySelector(".form-control").value.trim(),t=e.replace(/\s+/g,"-").toLowerCase(),t=(g.addBoards([{id:t,title:e}]),document.querySelector(".kanban-board:last-child"));if(t){const a=t.querySelector(".kanban-title-board"),n=(a.insertAdjacentHTML("afterend",b()),a.addEventListener("mouseenter",()=>{a.contentEditable="true"}),t.querySelector(".delete-board"));n&&n.addEventListener("click",()=>{var e=n.closest(".kanban-board").getAttribute("data-id");g.removeBoard(e)})}d.forEach(e=>{e.classList.add("d-none")}),v&&v.append(r)}),o.addEventListener("hidden.bs.offcanvas",()=>{var e=o.querySelector(".ql-editor").firstElementChild;e&&(e.innerHTML="")}),o&&o.addEventListener("shown.bs.offcanvas",()=>{Array.from(o.querySelectorAll('[data-bs-toggle="tooltip"]')).forEach(e=>{new bootstrap.Tooltip(e)})})}();